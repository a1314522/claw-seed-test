using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AssetManagementSystem.Core.Enums;

namespace AssetManagementSystem.Core.Models;

public class Asset
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string AssetCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public AssetCategory? Category { get; set; }

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

    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchasePrice { get; set; }

    public DateTime? PurchaseDate { get; set; }

    [MaxLength(100)]
    public string? Supplier { get; set; }

    [MaxLength(100)]
    public string? PurchaseOrderNo { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.在用;

    public int? PurchaseId { get; set; }

    [ForeignKey(nameof(PurchaseId))]
    public Purchase? Purchase { get; set; }

    public SyncStatus SyncStatus { get; set; } = SyncStatus.未同步;

    [MaxLength(100)]
    public string? KingdeeId { get; set; }

    [MaxLength(50)]
    public string? WarrantyPeriod { get; set; }

    public DateTime? WarrantyExpireDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ScrapDate { get; set; }

    [MaxLength(500)]
    public string? ScrapReason { get; set; }
}
