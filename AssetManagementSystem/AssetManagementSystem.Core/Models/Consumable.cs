using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementSystem.Core.Models;

public class Consumable
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public AssetCategory? Category { get; set; }

    [MaxLength(50)]
    public string? Specification { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    public int StockQuantity { get; set; }

    public int MinStock { get; set; } = 10;

    [MaxLength(100)]
    public string? StorageLocation { get; set; }

    [MaxLength(100)]
    public string? Supplier { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<ConsumableUsage> Usages { get; set; } = new List<ConsumableUsage>();
}
