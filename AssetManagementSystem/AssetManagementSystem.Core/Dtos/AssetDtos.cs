using System.ComponentModel.DataAnnotations;

namespace AssetManagementSystem.Core.Dtos;

public class AssetDto
{
    public int Id { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Location { get; set; }
    public string? Department { get; set; }
    public string? Owner { get; set; }
    public string? AdUserName { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? Supplier { get; set; }
    public string? PurchaseOrderNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? PurchaseId { get; set; }
    public string? PurchaseNo { get; set; }
    public string SyncStatus { get; set; } = string.Empty;
    public string? KingdeeId { get; set; }
    public string? WarrantyPeriod { get; set; }
    public DateTime? WarrantyExpireDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ScrapDate { get; set; }
    public string? ScrapReason { get; set; }
}

public class CreateAssetDto
{
    [Required]
    [MaxLength(50)]
    public string AssetCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int CategoryId { get; set; }

    [MaxLength(50)]
    public string? Brand { get; set; }

    [MaxLength(50)]
    public string? Model { get; set; }

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? Department { get; set; }

    [MaxLength(50)]
    public string? Owner { get; set; }

    [MaxLength(50)]
    public string? AdUserName { get; set; }

    public decimal PurchasePrice { get; set; }

    public DateTime? PurchaseDate { get; set; }

    [MaxLength(100)]
    public string? Supplier { get; set; }

    [MaxLength(100)]
    public string? PurchaseOrderNo { get; set; }

    public int? PurchaseId { get; set; }

    [MaxLength(50)]
    public string? WarrantyPeriod { get; set; }

    public DateTime? WarrantyExpireDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public string Status { get; set; } = "idle";
}

public class UpdateAssetDto : CreateAssetDto
{
    public int Id { get; set; }
}

public class AssetQueryDto
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public string? Status { get; set; }
    public string? Department { get; set; }
    public string? Location { get; set; }
    public DateTime? PurchaseDateFrom { get; set; }
    public DateTime? PurchaseDateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AssetListResultDto
{
    public List<AssetDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AssetCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int SortOrder { get; set; }
    public int AssetCount { get; set; }
    public List<AssetCategoryDto> Children { get; set; } = new();
}

public class CreateAssetCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
}

public class PurchaseDto
{
    public int Id { get; set; }
    public string PurchaseNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Applicant { get; set; }
    public string? Department { get; set; }
    public decimal TotalAmount { get; set; }
    public int Quantity { get; set; }
    public string? Supplier { get; set; }
    public DateTime? ApplyDate { get; set; }
    public DateTime? ApproveDate { get; set; }
    public string? Approver { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? ReceiveDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = new();
}

public class PurchaseItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Supplier { get; set; }
    public string? Notes { get; set; }
}

public class CreatePurchaseDto
{
    [Required]
    [MaxLength(50)]
    public string PurchaseNo { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Applicant { get; set; }

    [MaxLength(50)]
    public string? Department { get; set; }

    public decimal TotalAmount { get; set; }
    public int Quantity { get; set; }

    [MaxLength(100)]
    public string? Supplier { get; set; }

    public DateTime? ApplyDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public List<CreatePurchaseItemDto> Items { get; set; } = new();
}

public class CreatePurchaseItemDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    [MaxLength(50)]
    public string? Brand { get; set; }
    [MaxLength(50)]
    public string? Model { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    [MaxLength(100)]
    public string? Supplier { get; set; }
    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class ConsumableDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Specification { get; set; }
    public string? Unit { get; set; }
    public int StockQuantity { get; set; }
    public int MinStock { get; set; }
    public string? StorageLocation { get; set; }
    public string? Supplier { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ConsumableUsageDto
{
    public int Id { get; set; }
    public int ConsumableId { get; set; }
    public string? ConsumableName { get; set; }
    public int Quantity { get; set; }
    public string? User { get; set; }
    public string? Department { get; set; }
    public string? Purpose { get; set; }
    public DateTime UsageDate { get; set; }
    public string? Notes { get; set; }
}

public class CreateConsumableUsageDto
{
    public int ConsumableId { get; set; }
    public int Quantity { get; set; }
    [MaxLength(50)]
    public string? User { get; set; }
    [MaxLength(50)]
    public string? Department { get; set; }
    [MaxLength(200)]
    public string? Purpose { get; set; }
    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class DashboardStatisticsDto
{
    public int TotalAssets { get; set; }
    public int TotalCategories { get; set; }
    public int TotalConsumables { get; set; }
    public int ActiveAssets { get; set; }
    public int InRepairAssets { get; set; }
    public int ScrapAssets { get; set; }
    public int PendingPurchases { get; set; }
    public int LowStockConsumables { get; set; }
    public decimal TotalAssetValue { get; set; }
    public List<AssetStatusStatDto> StatusStats { get; set; } = new();
    public List<DepartmentStatDto> DepartmentStats { get; set; } = new();
    public List<CategoryStatDto> CategoryStats { get; set; } = new();
}

public class AssetStatusStatDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DepartmentStatDto
{
    public string Department { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

public class CategoryStatDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SystemConfigDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? Group { get; set; }
}

public class SyncConfigDto
{
    public string? KingdeeApiUrl { get; set; }
    public string? KingdeeAppId { get; set; }
    public string? KingdeeAppSecret { get; set; }
    public string? KingdeeOrgId { get; set; }
    public string? AdDomain { get; set; }
    public string? AdServer { get; set; }
    public string? AdUser { get; set; }
    public string? AdPassword { get; set; }
    public string? AdBaseDn { get; set; }
    public int? SyncIntervalMinutes { get; set; }
    public bool? EnableKingdeeSync { get; set; }
    public bool? EnableAdSync { get; set; }
}
