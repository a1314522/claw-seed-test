using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Enums;

namespace AssetManagementSystem.Core.Interfaces;

public interface IAssetService
{
    Task<AssetListResultDto> GetAssetsAsync(AssetQueryDto query);
    Task<AssetDto?> GetAssetByIdAsync(int id);
    Task<AssetDto?> GetAssetByCodeAsync(string code);
    Task<AssetDto> CreateAssetAsync(CreateAssetDto dto);
    Task<AssetDto> UpdateAssetAsync(int id, UpdateAssetDto dto);
    Task<bool> DeleteAssetAsync(int id);
    Task<bool> ScrapAssetAsync(int id, string reason);
    Task<bool> ChangeAssetStatusAsync(int id, AssetStatus status);
    Task<List<AssetDto>> GetAssetsByPurchaseIdAsync(int purchaseId);
    Task<bool> BatchImportAsync(List<CreateAssetDto> assets);
    Task<List<AssetDto>> ExportAsync(AssetQueryDto query);
}

public interface IAssetCategoryService
{
    Task<List<AssetCategoryDto>> GetCategoriesAsync();
    Task<AssetCategoryDto?> GetCategoryByIdAsync(int id);
    Task<AssetCategoryDto> CreateCategoryAsync(CreateAssetCategoryDto dto);
    Task<AssetCategoryDto> UpdateCategoryAsync(int id, CreateAssetCategoryDto dto);
    Task<bool> DeleteCategoryAsync(int id);
}

public interface IPurchaseService
{
    Task<List<PurchaseDto>> GetPurchasesAsync(string? status = null);
    Task<PurchaseDto?> GetPurchaseByIdAsync(int id);
    Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseDto dto);
    Task<PurchaseDto> ApprovePurchaseAsync(int id, string approver);
    Task<PurchaseDto> RejectPurchaseAsync(int id, string reason);
    Task<PurchaseDto> CompletePurchaseAsync(int id);
    Task<PurchaseDto> ReceivePurchaseAsync(int id);
    Task<bool> DeletePurchaseAsync(int id);
    Task<PurchaseDto> GenerateAssetsFromPurchaseAsync(int id);
}

public interface IConsumableService
{
    Task<List<ConsumableDto>> GetConsumablesAsync(string? keyword = null);
    Task<ConsumableDto?> GetConsumableByIdAsync(int id);
    Task<ConsumableDto> CreateConsumableAsync(ConsumableDto dto);
    Task<ConsumableDto> UpdateConsumableAsync(int id, ConsumableDto dto);
    Task<bool> DeleteConsumableAsync(int id);
    Task<ConsumableUsageDto> RecordUsageAsync(CreateConsumableUsageDto dto);
    Task<List<ConsumableUsageDto>> GetUsagesAsync(int consumableId);
    Task<List<ConsumableDto>> GetLowStockConsumablesAsync();
}

public interface IReportService
{
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync();
    Task<List<AssetStatusStatDto>> GetAssetStatusStatsAsync();
    Task<List<DepartmentStatDto>> GetDepartmentStatsAsync();
    Task<List<CategoryStatDto>> GetCategoryStatsAsync();
    Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName);
}

public interface ISystemConfigService
{
    Task<string?> GetConfigAsync(string key);
    Task<T?> GetConfigAsync<T>(string key);
    Task SetConfigAsync(string key, string value, string? description = null, string? group = null);
    Task<List<SystemConfigDto>> GetAllConfigsAsync(string? group = null);
    Task<SyncConfigDto> GetSyncConfigAsync();
    Task SaveSyncConfigAsync(SyncConfigDto config);
}

public interface IKingdeeSyncService
{
    Task<bool> TestConnectionAsync();
    Task<bool> SyncOrganizationsAsync();
    Task<bool> SyncAssetsAsync();
    Task<bool> SyncAssetCardsAsync();
}

public interface IAdSyncService
{
    Task<bool> TestConnectionAsync();
    Task<bool> SyncUsersAsync();
    Task<bool> SyncDepartmentsAsync();
    Task<List<string>> GetAdUsersAsync();
    Task<List<string>> GetAdDepartmentsAsync();
}
