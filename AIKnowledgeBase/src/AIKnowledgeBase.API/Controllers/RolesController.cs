using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Infrastructure.Data;

namespace AIKnowledgeBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolesController(AppDbContext context) => _context = context;

    [HttpGet]
    [Authorize(Policy = "RequireRoleManage")]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles()
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync();

        var items = roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList()
        }).ToList();

        return Ok(new ApiResponse<List<RoleDto>> { Data = items, TotalCount = items.Count });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "RequireRoleManage")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(int id)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return NotFound(new ApiResponse<RoleDto> { Success = false, Message = "角色不存在" });

        return Ok(new ApiResponse<RoleDto>
        {
            Data = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                Permissions = role.RolePermissions.Select(rp => rp.Permission.Name).ToList()
            }
        });
    }

    [HttpPost]
    [Authorize(Policy = "RequireRoleManage")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var role = new Role
        {
            Name = request.Name,
            Description = request.Description
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        if (request.PermissionIds?.Any() == true)
        {
            foreach (var permId in request.PermissionIds)
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permId });
            }
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetRole), new { id = role.Id },
            new ApiResponse<RoleDto> { Data = new RoleDto { Id = role.Id, Name = role.Name, Description = role.Description, CreatedAt = role.CreatedAt } });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "RequireRoleManage")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(int id, [FromBody] CreateRoleRequest request)
    {
        var role = await _context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound(new ApiResponse<RoleDto> { Success = false, Message = "角色不存在" });

        role.Name = request.Name;
        role.Description = request.Description;

        _context.RolePermissions.RemoveRange(role.RolePermissions);
        if (request.PermissionIds?.Any() == true)
        {
            foreach (var permId in request.PermissionIds)
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permId });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<RoleDto> { Data = new RoleDto { Id = role.Id, Name = role.Name, Description = role.Description, CreatedAt = role.CreatedAt } });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RequireRoleManage")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRole(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null) return NotFound(new ApiResponse<object> { Success = false, Message = "角色不存在" });

        if (role.Id <= 3)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "系统内置角色不可删除" });

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<object> { Message = "删除成功" });
    }

    [HttpGet("permissions")]
    [Authorize(Policy = "RequireRoleManage")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetPermissions()
    {
        var permissions = await _context.Permissions
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name, p.Description, p.Type })
            .ToListAsync();

        return Ok(new ApiResponse<List<object>> { Data = permissions.Cast<object>().ToList() });
    }
}
