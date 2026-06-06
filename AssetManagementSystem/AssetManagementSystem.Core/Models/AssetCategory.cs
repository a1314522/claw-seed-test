using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementSystem.Core.Models;

public class AssetCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public AssetCategory? Parent { get; set; }

    public ICollection<AssetCategory> Children { get; set; } = new List<AssetCategory>();

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
