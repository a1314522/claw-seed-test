using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AIKnowledgeBase.Core.DTOs;

namespace AIKnowledgeBase.Infrastructure.Identity;

public class JwtService
{
    private readonly string _secret;
    private readonly int _expiryMinutes;

    public JwtService(string secret, int expiryMinutes = 60)
    {
        _secret = secret;
        _expiryMinutes = expiryMinutes;
    }

    public string GenerateToken(UserInfo user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("is_admin", user.IsAdmin.ToString().ToLowerInvariant())
        };
        foreach (var perm in user.Permissions)
            claims.Add(new Claim("permission", perm));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "AIKnowledgeBase",
            audience: "AIKnowledgeBase",
            claims: claims,
            expires: DateTime.Now.AddMinutes(_expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
