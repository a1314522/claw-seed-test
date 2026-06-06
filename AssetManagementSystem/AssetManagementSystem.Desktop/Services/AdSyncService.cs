using AssetManagementSystem.Core.Interfaces;

namespace AssetManagementSystem.Desktop.Services;

public class AdSyncService : IAdSyncService
{
    public Task<bool> TestConnectionAsync()
    {
        // 预留：接入AD前返回模拟结果
        return Task.FromResult(true);
    }

    public Task<bool> SyncUsersAsync()
    {
        // 预留：从AD同步用户
        return Task.FromResult(true);
    }

    public Task<bool> SyncDepartmentsAsync()
    {
        // 预留：从AD同步部门
        return Task.FromResult(true);
    }

    public Task<List<string>> GetAdUsersAsync()
    {
        // 预留：返回模拟用户列表
        return Task.FromResult(new List<string> { "user1", "user2", "user3" });
    }

    public Task<List<string>> GetAdDepartmentsAsync()
    {
        // 预留：返回模拟部门列表
        return Task.FromResult(new List<string> { "IT部", "财务部", "人事部", "采购部" });
    }
}
