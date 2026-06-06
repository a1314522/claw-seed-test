using System.ComponentModel.DataAnnotations;

namespace AIKnowledgeBase.Core.Entities;

public class SearchHistory : BaseEntity
{
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string Answer { get; set; } = string.Empty;

    public string? Sources { get; set; }
}
