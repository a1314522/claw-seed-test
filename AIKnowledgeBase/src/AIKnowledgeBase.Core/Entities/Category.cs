using System.ComponentModel.DataAnnotations;

namespace AIKnowledgeBase.Core.Entities;

public class Category : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsPublic { get; set; } = true;

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    public virtual ICollection<UserCategoryAccess> UserAccesses { get; set; } = new List<UserCategoryAccess>();
}
