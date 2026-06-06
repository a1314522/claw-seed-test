using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AssetManagementSystem.Core.Enums;

namespace AssetManagementSystem.Core.Models;

public class Purchase
{
    [Key]
    public int Id { get; set; }

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

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public int Quantity { get; set; }

    [MaxLength(100)]
    public string? Supplier { get; set; }

    public DateTime? ApplyDate { get; set; } = DateTime.Now;

    public DateTime? ApproveDate { get; set; }

    [MaxLength(50)]
    public string? Approver { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public DateTime? ReceiveDate { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.待申请;

    [MaxLength(500)]
    public string? RejectReason { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
