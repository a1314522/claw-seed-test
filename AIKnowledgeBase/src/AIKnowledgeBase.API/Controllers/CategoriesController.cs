using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Core.Enums;
using AIKnowledgeBase.Infrastructure.Data;

namespace AIKnowledgeBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsPublic = c.IsPublic,
                CreatedAt = c.CreatedAt,
                DocumentCount = c.Documents.Count
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<CategoryDto>> { Data = categories });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound(new ApiResponse<CategoryDto> { Success = false, Message = "分类不存在" });

        var docCount = await _context.Documents.CountAsync(d => d.CategoryId == id);

        return Ok(new ApiResponse<CategoryDto>
        {
            Data = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsPublic = category.IsPublic,
                CreatedAt = category.CreatedAt,
                DocumentCount = docCount
            }
        });
    }

    [HttpPost]
    [Authorize(Policy = "RequireCategoryCreate")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            IsPublic = request.IsPublic
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id },
            new ApiResponse<CategoryDto> { Data = new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description, IsPublic = category.IsPublic, CreatedAt = category.CreatedAt } });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "RequireCategoryEdit")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound(new ApiResponse<CategoryDto> { Success = false, Message = "分类不存在" });

        if (request.Name != null) category.Name = request.Name;
        if (request.Description != null) category.Description = request.Description;
        if (request.IsPublic.HasValue) category.IsPublic = request.IsPublic.Value;

        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<CategoryDto> { Data = new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description, IsPublic = category.IsPublic, CreatedAt = category.CreatedAt } });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RequireCategoryDelete")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id)
    {
        if (id == 1)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "默认分类不可删除" });

        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound(new ApiResponse<object> { Success = false, Message = "分类不存在" });

        // Move documents to default category
        var docs = await _context.Documents.Where(d => d.CategoryId == id).ToListAsync();
        foreach (var doc in docs) doc.CategoryId = 1;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<object> { Message = "删除成功" });
    }
}
