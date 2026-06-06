using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Core.Enums;
using AIKnowledgeBase.Core.Interfaces;
using AIKnowledgeBase.Infrastructure.Data;
using AIKnowledgeBase.Infrastructure.Identity;

namespace AIKnowledgeBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly IAuthService _authService;

    public UsersController(AppDbContext context, PasswordHasher passwordHasher, IAuthService authService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _authService = authService;
    }

    [HttpGet]
    [Authorize(Policy = "RequireUserView")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var total = await _context.Users.CountAsync();
        var users = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            IsAdmin = u.IsAdmin,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
        }).ToList();

        return Ok(new ApiResponse<PagedResult<UserDto>>
        {
            Data = new PagedResult<UserDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize }
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "RequireUserView")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound(new ApiResponse<UserDto> { Success = false, Message = "用户不存在" });

        return Ok(new ApiResponse<UserDto>
        {
            Data = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                IsAdmin = user.IsAdmin,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            }
        });
    }

    [HttpPost]
    [Authorize(Policy = "RequireUserCreate")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new ApiResponse<UserDto> { Success = false, Message = "用户名已存在" });

        var user = new User
        {
            Username = request.Username,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            IsAdmin = request.IsAdmin,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (request.RoleIds?.Any() == true)
        {
            foreach (var roleId in request.RoleIds)
            {
                _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            }
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetUser), new { id = user.Id },
            new ApiResponse<UserDto> { Data = new UserDto { Id = user.Id, Username = user.Username, IsAdmin = user.IsAdmin, IsActive = true, CreatedAt = user.CreatedAt } });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "RequireUserEdit")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound(new ApiResponse<UserDto> { Success = false, Message = "用户不存在" });

        user.IsAdmin = request.IsAdmin;
        user.IsActive = request.IsActive;

        if (request.RoleIds?.Any() == true)
        {
            _context.UserRoles.RemoveRange(user.UserRoles);
            foreach (var roleId in request.RoleIds)
            {
                _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<UserDto> { Data = new UserDto { Id = user.Id, Username = user.Username, IsAdmin = user.IsAdmin, IsActive = user.IsActive, CreatedAt = user.CreatedAt } });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RequireUserDelete")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new ApiResponse<object> { Success = false, Message = "用户不存在" });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object> { Message = "删除成功" });
    }
}
