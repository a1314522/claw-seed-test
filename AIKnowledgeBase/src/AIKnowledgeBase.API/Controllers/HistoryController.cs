using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Infrastructure.Data;
using System.Text.Json;

namespace AIKnowledgeBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public HistoryController(AppDbContext context) => _context = context;

    [HttpGet]
    [Authorize(Policy = "RequireHistoryView")]
    public async Task<ActionResult<ApiResponse<PagedResult<SearchHistoryDto>>>> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized(new ApiResponse<PagedResult<SearchHistoryDto>> { Success = false, Message = "未登录" });

        var total = await _context.SearchHistories.CountAsync(h => h.UserId == userId.Value);
        var history = await _context.SearchHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId.Value)
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = history.Select(h =>
        {
            var sources = string.IsNullOrEmpty(h.Sources)
                ? new List<SourceDto>()
                : JsonSerializer.Deserialize<List<SourceDto>>(h.Sources) ?? new List<SourceDto>();

            return new SearchHistoryDto
            {
                Id = h.Id,
                Question = h.Question,
                Answer = h.Answer,
                Sources = sources,
                CreatedAt = h.CreatedAt
            };
        }).ToList();

        return Ok(new ApiResponse<PagedResult<SearchHistoryDto>>
        {
            Data = new PagedResult<SearchHistoryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize }
        });
    }

    [HttpDelete]
    [Authorize(Policy = "RequireHistoryClear")]
    public async Task<ActionResult<ApiResponse<object>>> ClearHistory()
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Unauthorized(new ApiResponse<object> { Success = false, Message = "未登录" });

        var records = await _context.SearchHistories.Where(h => h.UserId == userId.Value).ToListAsync();
        _context.SearchHistories.RemoveRange(records);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object> { Message = "历史记录已清空" });
    }

    private int? GetUserId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var result) ? result : null;
    }
}
