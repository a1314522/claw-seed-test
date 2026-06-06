using AssetManagementSystem.Core.Interfaces;

namespace AssetManagementSystem.Desktop.Services;

public class KingdeeSyncService : IKingdeeSyncService
{
    public Task<bool> TestConnectionAsync()
    {
        // 预留：接入金蝶API前返回模拟结果
        return Task.FromResult(true);
    }

    public Task<bool> SyncOrganizationsAsync()
    {
        // 预留：从金蝶同步组织架构
        return Task.FromResult(true);
    }

    public Task<bool> SyncAssetsAsync()
    {
        // 预留：从金蝶同步资产数据
        return Task.FromResult(true);
    }

    public Task<bool> SyncAssetCardsAsync()
    {
        // 预留：同步资产卡片到金蝶
        return Task.FromResult(true);
    }
}
