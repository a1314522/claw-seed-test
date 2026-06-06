using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Core.Enums;
using AIKnowledgeBase.Core.Interfaces;
using AIKnowledgeBase.Infrastructure.Data;

namespace AIKnowledgeBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDocumentParser _parser;
    private readonly ILLMService _llmService;
    private readonly ISearchEngine _searchEngine;
    private readonly IConfiguration _config;

    public DocumentsController(AppDbContext context, IDocumentParser parser, ILLMService llmService, ISearchEngine searchEngine, IConfiguration config)
    {
        _context = context;
        _parser = parser;
        _llmService = llmService;
        _searchEngine = searchEngine;
        _config = config;
    }

    [HttpGet]
    [Authorize(Policy = "RequireDocumentView")]
    public async Task<ActionResult<ApiResponse<PagedResult<DocumentDto>>>> GetDocuments([FromQuery] DocumentFilterRequest filter)
    {
        var query = _context.Documents.AsNoTracking().AsQueryable();

        if (filter.CategoryId.HasValue)
            query = query.Where(d => d.CategoryId == filter.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(d => d.OriginalName.Contains(filter.Search) || d.FileName.Contains(filter.Search));

        var total = await query.CountAsync();
        var docs = await query
            .Include(d => d.Category)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = docs.Select(d => new DocumentDto
        {
            Id = d.Id,
            FileName = d.FileName,
            OriginalName = d.OriginalName,
            FileSize = d.FileSize,
            DocType = d.DocType,
            CategoryId = d.CategoryId,
            CategoryName = d.Category?.Name,
            ChunkCount = d.ChunkCount,
            Status = d.Status.ToString(),
            UploadedBy = d.UploadedBy,
            CreatedAt = d.CreatedAt
        }).ToList();

        return Ok(new ApiResponse<PagedResult<DocumentDto>>
        {
            Data = new PagedResult<DocumentDto> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize }
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "RequireDocumentView")]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> GetDocument(int id)
    {
        var doc = await _context.Documents
            .AsNoTracking()
            .Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doc == null) return NotFound(new ApiResponse<DocumentDto> { Success = false, Message = "文档不存在" });

        return Ok(new ApiResponse<DocumentDto>
        {
            Data = new DocumentDto
            {
                Id = doc.Id,
                FileName = doc.FileName,
                OriginalName = doc.OriginalName,
                FileSize = doc.FileSize,
                DocType = doc.DocType,
                CategoryId = doc.CategoryId,
                CategoryName = doc.Category?.Name,
                ChunkCount = doc.ChunkCount,
                Status = doc.Status.ToString(),
                UploadedBy = doc.UploadedBy,
                CreatedAt = doc.CreatedAt
            }
        });
    }

    [HttpPost("upload")]
    [Authorize(Policy = "RequireDocumentUpload")]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> Upload([FromForm] IFormFile file, [FromForm] int categoryId = 1)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ApiResponse<DocumentDto> { Success = false, Message = "请选择要上传的文件" });

        var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
        var uploadPath = _config.GetValue<string>("FileStorage:UploadPath") ?? "App_Data/Uploads";
        Directory.CreateDirectory(uploadPath);
        var filePath = Path.Combine(uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var doc = new Document
        {
            FileName = fileName,
            OriginalName = file.FileName,
            FileSize = file.Length,
            DocType = Path.GetExtension(file.FileName).ToLowerInvariant(),
            CategoryId = categoryId,
            Status = DocumentStatus.Pending,
            UploadedBy = User.Identity?.Name
        };

        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        // Process document in background
        _ = Task.Run(async () => await ProcessDocumentAsync(doc.Id, filePath));

        return Ok(new ApiResponse<DocumentDto>
        {
            Data = new DocumentDto
            {
                Id = doc.Id,
                FileName = doc.FileName,
                OriginalName = doc.OriginalName,
                FileSize = doc.FileSize,
                DocType = doc.DocType,
                CategoryId = doc.CategoryId,
                Status = doc.Status.ToString(),
                UploadedBy = doc.UploadedBy,
                CreatedAt = doc.CreatedAt
            },
            Message = "上传成功，正在处理文档内容"
        });
    }

    [HttpPut("{id:int}/category")]
    [Authorize(Policy = "RequireDocumentManage")]
    public async Task<ActionResult<ApiResponse<DocumentDto>>> UpdateCategory(int id, [FromBody] UpdateDocumentCategoryRequest request)
    {
        var doc = await _context.Documents.FindAsync(id);
        if (doc == null) return NotFound(new ApiResponse<DocumentDto> { Success = false, Message = "文档不存在" });

        doc.CategoryId = request.CategoryId;
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<DocumentDto> { Data = new DocumentDto { Id = doc.Id, FileName = doc.FileName, OriginalName = doc.OriginalName, CategoryId = doc.CategoryId, Status = doc.Status.ToString() } });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RequireDocumentDelete")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDocument(int id)
    {
        var doc = await _context.Documents.Include(d => d.Chunks).FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return NotFound(new ApiResponse<object> { Success = false, Message = "文档不存在" });

        var uploadPath = _config.GetValue<string>("FileStorage:UploadPath") ?? "App_Data/Uploads";
        var filePath = Path.Combine(uploadPath, doc.FileName);
        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

        _context.DocumentChunks.RemoveRange(doc.Chunks);
        _context.Documents.Remove(doc);
        await _context.SaveChangesAsync();

        await _searchEngine.RemoveDocumentAsync(id);

        return Ok(new ApiResponse<object> { Message = "删除成功" });
    }

    private async Task ProcessDocumentAsync(int docId, string filePath)
    {
        try
        {
            var doc = await _context.Documents.FindAsync(docId);
            if (doc == null) return;

            doc.Status = DocumentStatus.Processing;
            await _context.SaveChangesAsync();

            if (!_parser.CanParse(filePath))
            {
                doc.Status = DocumentStatus.Error;
                await _context.SaveChangesAsync();
                return;
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var text = await _parser.ExtractTextAsync(stream, filePath);
            var chunks = ChunkText(text, 1000, 200);

            for (int i = 0; i < chunks.Count; i++)
            {
                _context.DocumentChunks.Add(new DocumentChunk
                {
                    DocumentId = docId,
                    ChunkIndex = i,
                    Text = chunks[i]
                });
            }

            doc.ChunkCount = chunks.Count;
            doc.Status = DocumentStatus.Done;
            await _context.SaveChangesAsync();
            await _searchEngine.AddDocumentAsync(docId, chunks);
        }
        catch
        {
            var doc = await _context.Documents.FindAsync(docId);
            if (doc != null)
            {
                doc.Status = DocumentStatus.Error;
                await _context.SaveChangesAsync();
            }
        }
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        if (string.IsNullOrEmpty(text)) return chunks;
        if (text.Length <= chunkSize) { chunks.Add(text); return chunks; }

        int start = 0;
        while (start < text.Length)
        {
            int len = Math.Min(chunkSize, text.Length - start);
            chunks.Add(text.Substring(start, len));
            start += chunkSize - overlap;
            if (start >= text.Length - overlap) break;
        }
        return chunks;
    }
}
