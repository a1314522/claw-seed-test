using System.ComponentModel.DataAnnotations;

namespace AIKnowledgeBase.Core.Entities;

public class User : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsAdmin { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserCategoryAccess> CategoryAccesses { get; set; } = new List<UserCategoryAccess>();
    public virtual ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
}
