using AIKnowledgeBase.Core.DTOs;

namespace AIKnowledgeBase.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<UserInfo?> GetCurrentUserAsync(string username);
    List<string> GetUserPermissions(int userId);
}
