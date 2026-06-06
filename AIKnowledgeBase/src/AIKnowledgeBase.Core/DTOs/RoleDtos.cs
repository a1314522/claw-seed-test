using System.ComponentModel.DataAnnotations;

namespace AIKnowledgeBase.Core.DTOs;

public class CreateRoleRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public List<int> PermissionIds { get; set; } = new();
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class AssignRoleRequest
{
    public List<int> RoleIds { get; set; } = new();
}
