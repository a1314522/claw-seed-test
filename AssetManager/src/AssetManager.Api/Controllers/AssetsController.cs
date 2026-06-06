using AssetManager.Core.Entities;
using AssetManager.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly AssetManagerDbContext _context;

    public AssetsController(AssetManagerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Asset>>> GetAssets(
        [FromQuery] string? keyword,
        [FromQuery] AssetStatus? status,
        [FromQuery] int? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Assets
            .Include(a => a.CurrentUser)
            .Include(a => a.CurrentDepartment)
            .AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(a => a.Name.Contains(keyword) || a.AssetCode.Contains(keyword));

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (departmentId.HasValue)
            query = query.Where(a => a.CurrentDepartmentId == departmentId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, items });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Asset>> GetAsset(int id)
    {
        var asset = await _context.Assets
            .Include(a => a.LifecycleRecords)
            .Include(a => a.CurrentUser)
            .Include(a => a.CurrentDepartment)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asset == null) return NotFound();
        return Ok(asset);
    }

    [HttpPost]
    public async Task<ActionResult<Asset>> CreateAsset(Asset asset)
    {
        asset.AssetCode = await GenerateAssetCode(asset.Category);
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // 记录入库
        _context.AssetLifecycles.Add(new AssetLifecycle
        {
            AssetId = asset.Id,
            Action = LifecycleAction.StockIn,
            ActionTime = DateTime.Now,
            OperatorName = User.Identity?.Name ?? "系统",
            Remarks = "资产入库（从金蝶同步或手工录入）"
        });
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, asset);
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignAsset(int id, [FromBody] AssignRequest request)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null) return NotFound();

        asset.Status = AssetStatus.InUse;
        asset.CurrentUserId = request.UserId;
        asset.CurrentDepartmentId = request.DepartmentId;
        asset.Location = request.Location;
        asset.UpdatedAt = DateTime.Now;

        _context.AssetLifecycles.Add(new AssetLifecycle
        {
            AssetId = id,
            Action = LifecycleAction.Assigned,
            ActionTime = DateTime.Now,
            OperatorName = User.Identity?.Name ?? "系统",
            ToUser = request.UserName,
            ToDepartment = request.DepartmentName,
            Remarks = request.Remarks
        });

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id}/transfer")]
    public async Task<IActionResult> TransferAsset(int id, [FromBody] TransferRequest request)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null) return NotFound();

        var oldUser = asset.CurrentUser?.Name;
        var oldDept = asset.CurrentDepartment?.Name;

        asset.CurrentUserId = request.NewUserId;
        asset.CurrentDepartmentId = request.NewDepartmentId;
        asset.Location = request.NewLocation;
        asset.UpdatedAt = DateTime.Now;

        _context.AssetLifecycles.Add(new AssetLifecycle
        {
            AssetId = id,
            Action = LifecycleAction.Transferred,
            ActionTime = DateTime.Now,
            OperatorName = User.Identity?.Name ?? "系统",
            FromUser = oldUser,
            ToUser = request.NewUserName,
            FromDepartment = oldDept,
            ToDepartment = request.NewDepartmentName,
            Remarks = request.Remarks
        });

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id}/scrap")]
    public async Task<IActionResult> ScrapAsset(int id, [FromBody] ScrapRequest request)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null) return NotFound();

        asset.Status = AssetStatus.Scrapped;
        asset.UpdatedAt = DateTime.Now;

        _context.AssetLifecycles.Add(new AssetLifecycle
        {
            AssetId = id,
            Action = LifecycleAction.Scrapped,
            ActionTime = DateTime.Now,
            OperatorName = User.Identity?.Name ?? "系统",
            Remarks = request.Remarks
        });

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("{id}/qrcode")]
    public async Task<IActionResult> GetQrCode(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null) return NotFound();

        // 返回资产二维码数据（前端渲染或后端生成图片）
        var qrData = $"asset:{asset.AssetCode}:{asset.Id}";
        return Ok(new { qrData, assetName = asset.Name, assetCode = asset.AssetCode });
    }

    private async Task<string> GenerateAssetCode(string category)
    {
        var prefix = category switch
        {
            "电脑" => "PC",
            "服务器" => "SV",
            "网络设备" => "NW",
            "打印机" => "PR",
            "办公家具" => "OF",
            _ => "AS"
        };

        var date = DateTime.Now.ToString("yyyyMM");
        var count = await _context.Assets.CountAsync(a => a.AssetCode.StartsWith($"{prefix}-{date}")) + 1;
        return $"{prefix}-{date}-{count:D4}";
    }
}

public class AssignRequest
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Remarks { get; set; }
}

public class TransferRequest
{
    public int NewUserId { get; set; }
    public string NewUserName { get; set; } = string.Empty;
    public int NewDepartmentId { get; set; }
    public string NewDepartmentName { get; set; } = string.Empty;
    public string? NewLocation { get; set; }
    public string? Remarks { get; set; }
}

public class ScrapRequest
{
    public string? Remarks { get; set; }
}
