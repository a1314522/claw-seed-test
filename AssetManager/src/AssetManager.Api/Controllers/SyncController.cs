using AssetManager.Core.Entities;
using AssetManager.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly AssetManagerDbContext _context;

    public SyncController(AssetManagerDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 手动触发从金蝶同步组织架构
    /// </summary>
    [HttpPost("kingdee/organizations")]
    public async Task<IActionResult> SyncOrganizations()
    {
        // TODO: 调用金蝶API获取部门列表
        // 这里预留接口，实际实现需要接入金蝶K3 Cloud API
        var log = new KingdeeSyncLog
        {
            SyncType = "Department",
            SyncTime = DateTime.Now,
            Result = "Success",
            RecordCount = 0,
            Remarks = "手动触发同步（待接入金蝶API）"
        };

        _context.KingdeeSyncLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new { message = "同步任务已触发，请查看同步日志", logId = log.Id });
    }

    /// <summary>
    /// 手动触发从金蝶同步资产卡片
    /// </summary>
    [HttpPost("kingdee/assets")]
    public async Task<IActionResult> SyncAssets([FromBody] SyncAssetsRequest request)
    {
        // TODO: 调用金蝶API获取资产卡片
        // 同步逻辑：
        // 1. 获取金蝶资产卡片列表（按更新时间过滤）
        // 2. 对比本地数据，新增或更新
        // 3. 生成资产生命周期记录（入库）

        var log = new KingdeeSyncLog
        {
            SyncType = "AssetCard",
            SyncTime = DateTime.Now,
            Result = "Success",
            RecordCount = request.LastSyncTime.HasValue ? 0 : 100, // 全量同步示例
            Remarks = $"同步时间范围：{request.LastSyncTime?.ToString("yyyy-MM-dd") ?? "全量"}"
        };

        _context.KingdeeSyncLogs.Add(log);
        await _context.SaveChangesAsync();

        // 更新最后同步时间配置
        var config = await _context.SystemConfigs.FirstOrDefaultAsync(s => s.ConfigKey == "Kingdee.LastSyncTime");
        if (config != null)
        {
            config.ConfigValue = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            config.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "资产卡片同步已触发", logId = log.Id });
    }

    [HttpGet("kingdee/logs")]
    public async Task<IActionResult> GetSyncLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var logs = await _context.KingdeeSyncLogs
            .OrderByDescending(l => l.SyncTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(logs);
    }
}

public class SyncAssetsRequest
{
    public DateTime? LastSyncTime { get; set; }
}
