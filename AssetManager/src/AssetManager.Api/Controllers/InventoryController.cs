using AssetManager.Core.Entities;
using AssetManager.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly AssetManagerDbContext _context;

    public InventoryController(AssetManagerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetInventoryChecks()
    {
        var items = await _context.InventoryChecks
            .Include(i => i.Items)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryCheck>> CreateInventoryCheck([FromBody] CreateCheckRequest request)
    {
        var check = new InventoryCheck
        {
            Name = request.Name,
            Scope = request.Scope,
            PlannedStartTime = request.PlannedStartTime,
            CreatedBy = User.Identity?.Name ?? "系统"
        };

        _context.InventoryChecks.Add(check);
        await _context.SaveChangesAsync();

        // 根据范围生成盘点明细
        var assets = await _context.Assets.ToListAsync();
        foreach (var asset in assets)
        {
            _context.InventoryCheckItems.Add(new InventoryCheckItem
            {
                InventoryCheckId = check.Id,
                AssetId = asset.Id,
                BookStatus = asset.Status
            });
        }
        await _context.SaveChangesAsync();

        return Ok(check);
    }

    [HttpPost("{id}/scan")]
    public async Task<IActionResult> ScanAsset(int id, [FromBody] ScanRequest request)
    {
        var item = await _context.InventoryCheckItems
            .Include(i => i.Asset)
            .FirstOrDefaultAsync(i => i.InventoryCheckId == id && i.Asset.AssetCode == request.AssetCode);

        if (item == null)
        {
            // 盘盈：资产不在盘点列表中但扫码发现
            return Ok(new { result = "surplus", message = "盘盈：该资产不在本次盘点范围内" });
        }

        item.Result = CheckResult.Normal;
        item.CheckerName = request.CheckerName;
        item.CheckTime = DateTime.Now;

        await _context.SaveChangesAsync();
        return Ok(new { result = "normal", assetName = item.Asset.Name });
    }

    [HttpPost("{id}/mark-missing")]
    public async Task<IActionResult> MarkMissing(int id, [FromBody] MarkMissingRequest request)
    {
        var item = await _context.InventoryCheckItems
            .FirstOrDefaultAsync(i => i.InventoryCheckId == id && i.AssetId == request.AssetId);

        if (item == null) return NotFound();

        item.Result = CheckResult.Shortage;
        item.Remarks = request.Remarks;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("{id}/report")]
    public async Task<IActionResult> GetReport(int id)
    {
        var check = await _context.InventoryChecks
            .Include(i => i.Items)
            .ThenInclude(item => item.Asset)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (check == null) return NotFound();

        var total = check.Items.Count;
        var checked = check.Items.Count(i => i.Result != CheckResult.NotChecked);
        var normal = check.Items.Count(i => i.Result == CheckResult.Normal);
        var shortage = check.Items.Count(i => i.Result == CheckResult.Shortage);
        var surplus = 0; // 盘盈需要另外统计

        return Ok(new
        {
            check.Name,
            total,
            checked,
            normal,
            shortage,
            surplus,
            uncheckedItems = check.Items.Where(i => i.Result == CheckResult.NotChecked).Select(i => new { i.Asset.AssetCode, i.Asset.Name })
        });
    }
}

public class CreateCheckRequest
{
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = "All";
    public DateTime? PlannedStartTime { get; set; }
}

public class ScanRequest
{
    public string AssetCode { get; set; } = string.Empty;
    public string CheckerName { get; set; } = string.Empty;
}

public class MarkMissingRequest
{
    public int AssetId { get; set; }
    public string? Remarks { get; set; }
}
