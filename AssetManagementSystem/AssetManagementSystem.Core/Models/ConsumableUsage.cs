using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementSystem.Core.Models;

public class ConsumableUsage
{
    [Key]
    public int Id { get; set; }

    public int ConsumableId { get; set; }

    [ForeignKey(nameof(ConsumableId))]
    public Consumable? Consumable { get; set; }

    public int Quantity { get; set; }

    [MaxLength(50)]
    public string? User { get; set; }

    [MaxLength(50)]
    public string? Department { get; set; }

    [MaxLength(200)]
    public string? Purpose { get; set; }

    public DateTime UsageDate { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
