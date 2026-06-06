using System.Security.Claims;
using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIKnowledgeBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<LoginResponse> { Success = false, Message = "请求参数无效" });

        var result = await authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new ApiResponse<LoginResponse> { Success = false, Message = "用户名或密码错误" });

        return Ok(new ApiResponse<LoginResponse> { Data = result, Message = "登录成功" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetCurrentUser()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new ApiResponse<UserInfo> { Success = false, Message = "未登录" });

        var user = await authService.GetCurrentUserAsync(username);
        if (user == null)
            return NotFound(new ApiResponse<UserInfo> { Success = false, Message = "用户不存在" });

        return Ok(new ApiResponse<UserInfo> { Data = user });
    }
}
