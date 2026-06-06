using System.ComponentModel.DataAnnotations;
using AIKnowledgeBase.Core.Enums;

namespace AIKnowledgeBase.Core.Entities;

public class Permission : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public PermissionType Type { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
