using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Interfaces;
using AssetManagementSystem.Core.Models;
using AssetManagementSystem.Desktop.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssetManagementSystem.Desktop.Services;

public class SystemConfigService(AppDbContext context) : ISystemConfigService
{
    public async Task<string?> GetConfigAsync(string key)
    {
        var config = await context.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
        return config?.Value;
    }

    public async Task<T?> GetConfigAsync<T>(string key)
    {
        var val = await GetConfigAsync(key);
        if (string.IsNullOrWhiteSpace(val)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(val);
        }
        catch
        {
            return default;
        }
    }

    public async Task SetConfigAsync(string key, string value, string? description = null, string? group = null)
    {
        var config = await context.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
        if (config == null)
        {
            config = new SystemConfig { Key = key, Value = value, Description = description, Group = group };
            context.SystemConfigs.Add(config);
        }
        else
        {
            config.Value = value;
            if (description != null) config.Description = description;
            if (group != null) config.Group = group;
            config.UpdatedAt = DateTime.Now;
        }
        await context.SaveChangesAsync();
    }

    public async Task<List<SystemConfigDto>> GetAllConfigsAsync(string? group = null)
    {
        var q = context.SystemConfigs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(group))
            q = q.Where(c => c.Group == group);
        return await q.Select(c => new SystemConfigDto
        {
            Id = c.Id,
            Key = c.Key,
            Value = c.Value,
            Description = c.Description,
            Group = c.Group
        }).ToListAsync();
    }

    public async Task<SyncConfigDto> GetSyncConfigAsync()
    {
        var keys = new[] { "KingdeeApiUrl", "KingdeeAppId", "KingdeeAppSecret", "KingdeeOrgId",
            "AdDomain", "AdServer", "AdUser", "AdPassword", "AdBaseDn", "SyncIntervalMinutes", "EnableKingdeeSync", "EnableAdSync" };

        var dict = new Dictionary<string, string?>();
        foreach (var key in keys)
        {
            dict[key] = await GetConfigAsync($"Sync.{key}");
        }

        return new SyncConfigDto
        {
            KingdeeApiUrl = dict["KingdeeApiUrl"],
            KingdeeAppId = dict["KingdeeAppId"],
            KingdeeAppSecret = dict["KingdeeAppSecret"],
            KingdeeOrgId = dict["KingdeeOrgId"],
            AdDomain = dict["AdDomain"],
            AdServer = dict["AdServer"],
            AdUser = dict["AdUser"],
            AdPassword = dict["AdPassword"],
            AdBaseDn = dict["AdBaseDn"],
            SyncIntervalMinutes = string.IsNullOrWhiteSpace(dict["SyncIntervalMinutes"]) ? null : int.Parse(dict["SyncIntervalMinutes"]!),
            EnableKingdeeSync = string.IsNullOrWhiteSpace(dict["EnableKingdeeSync"]) ? null : bool.Parse(dict["EnableKingdeeSync"]!),
            EnableAdSync = string.IsNullOrWhiteSpace(dict["EnableAdSync"]) ? null : bool.Parse(dict["EnableAdSync"]!)
        };
    }

    public async Task SaveSyncConfigAsync(SyncConfigDto config)
    {
        if (config.KingdeeApiUrl != null) await SetConfigAsync("Sync.KingdeeApiUrl", config.KingdeeApiUrl, "金蝶API地址", "Sync");
        if (config.KingdeeAppId != null) await SetConfigAsync("Sync.KingdeeAppId", config.KingdeeAppId, "金蝶AppID", "Sync");
        if (config.KingdeeAppSecret != null) await SetConfigAsync("Sync.KingdeeAppSecret", config.KingdeeAppSecret, "金蝶AppSecret", "Sync");
        if (config.KingdeeOrgId != null) await SetConfigAsync("Sync.KingdeeOrgId", config.KingdeeOrgId, "金蝶组织ID", "Sync");
        if (config.AdDomain != null) await SetConfigAsync("Sync.AdDomain", config.AdDomain, "AD域", "Sync");
        if (config.AdServer != null) await SetConfigAsync("Sync.AdServer", config.AdServer, "AD服务器", "Sync");
        if (config.AdUser != null) await SetConfigAsync("Sync.AdUser", config.AdUser, "AD用户名", "Sync");
        if (config.AdPassword != null) await SetConfigAsync("Sync.AdPassword", config.AdPassword, "AD密码", "Sync");
        if (config.AdBaseDn != null) await SetConfigAsync("Sync.AdBaseDn", config.AdBaseDn, "AD基础DN", "Sync");
        if (config.SyncIntervalMinutes.HasValue) await SetConfigAsync("Sync.SyncIntervalMinutes", config.SyncIntervalMinutes.Value.ToString(), "同步间隔(分钟)", "Sync");
        if (config.EnableKingdeeSync.HasValue) await SetConfigAsync("Sync.EnableKingdeeSync", config.EnableKingdeeSync.Value.ToString(), "启用金蝶同步", "Sync");
        if (config.EnableAdSync.HasValue) await SetConfigAsync("Sync.EnableAdSync", config.EnableAdSync.Value.ToString(), "启用AD同步", "Sync");
    }
}
