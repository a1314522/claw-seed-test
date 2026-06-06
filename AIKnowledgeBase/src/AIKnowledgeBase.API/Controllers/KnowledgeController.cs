using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Core.Interfaces;
using AIKnowledgeBase.Infrastructure.Data;

namespace AIKnowledgeBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KnowledgeController : ControllerBase
{
    private readonly ISearchEngine _searchEngine;
    private readonly ILLMService _llmService;
    private readonly AppDbContext _context;

    public KnowledgeController(ISearchEngine searchEngine, ILLMService llmService, AppDbContext context)
    {
        _searchEngine = searchEngine;
        _llmService = llmService;
        _context = context;
    }

    [HttpPost("search")]
    [Authorize(Policy = "RequireDocumentView")]
    public async Task<ActionResult<ApiResponse<SearchResultDto>>> Search([FromBody] SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new ApiResponse<SearchResultDto> { Success = false, Message = "请输入搜索问题" });

        var sources = await _searchEngine.SearchAsync(request.Question, request.CategoryId, request.TopK);

        // Fetch full text for LLM context
        var contexts = new List<string>();
        foreach (var source in sources)
        {
            if (source.DocumentId.HasValue)
            {
                var chunks = await _context.DocumentChunks
                    .Where(c => c.DocumentId == source.DocumentId.Value)
                    .OrderBy(c => c.ChunkIndex)
                    .Take(2)
                    .Select(c => c.Text)
                    .ToListAsync();
                contexts.AddRange(chunks);
            }
        }

        var answer = await _llmService.GenerateAnswerAsync(request.Question, contexts);

        var result = new SearchResultDto
        {
            Answer = answer,
            IsMock = !_llmService.IsAvailable,
            Sources = sources
        };

        // Save history
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                var sourcesJson = System.Text.Json.JsonSerializer.Serialize(sources);
                _context.SearchHistories.Add(new SearchHistory
                {
                    UserId = userId,
                    Question = request.Question,
                    Answer = answer,
                    Sources = sourcesJson
                });
                await _context.SaveChangesAsync();
            }
        }
        catch { /* Ignore history save failures */ }

        return Ok(new ApiResponse<SearchResultDto> { Data = result });
    }

    [HttpPost("ask")]
    [Authorize(Policy = "RequireDocumentView")]
    public async Task<ActionResult<ApiResponse<SearchResultDto>>> Ask([FromBody] SearchRequest request)
    {
        return await Search(request);
    }
}
