using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Interfaces;
using AIKnowledgeBase.Infrastructure.Data;
using AIKnowledgeBase.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeBase.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtService _jwtService;

    public AuthService(AppDbContext context, PasswordHasher passwordHasher, JwtService jwtService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        var userInfo = await GetCurrentUserAsync(request.Username);
        if (userInfo == null) return null;

        var token = _jwtService.GenerateToken(userInfo);
        return new LoginResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = 3600,
            User = userInfo
        };
    }

    public async Task<UserInfo?> GetCurrentUserAsync(string username)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null) return null;

        var permissions = GetUserPermissions(user.Id);

        return new UserInfo
        {
            Id = user.Id,
            Username = user.Username,
            IsAdmin = user.IsAdmin,
            Permissions = permissions
        };
    }

    public List<string> GetUserPermissions(int userId)
    {
        var user = _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefault(u => u.Id == userId);

        if (user == null) return new List<string>();

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        return permissions;
    }
}
