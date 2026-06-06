using Microsoft.AspNetCore.Mvc;
using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;

namespace AIKnowledgeBase.API.Controllers.v1;

[ApiController]
[Route("api/v1/am/assets")]
[Produces("application/json")]
[AllowAnonymous]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(IAssetService assetService, ILogger<AssetsController> logger)
    {
        _assetService = assetService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<Asset>>> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var assets = await _assetService.GetAllAsync(category, status, page, pageSize);
        var stats = await _assetService.GetStatisticsAsync();
        
        return Ok(new PagedResponse<Asset>
        {
            Items = assets.ToList(),
            Total = stats["total"],
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Asset>> GetById(Guid id)
    {
        var asset = await _assetService.GetByIdAsync(id);
        if (asset == null) return NotFound();
        return Ok(asset);
    }

    [HttpPost]
    public async Task<ActionResult<Asset>> Create([FromBody] CreateAssetRequest request)
    {
        try
        {
            var asset = new Asset
            {
                AssetCode = request.AssetCode,
                AssetName = request.AssetName,
                AssetType = request.AssetType,
                Category = request.Category,
                DepartmentId = request.DepartmentId,
                UserId = request.UserId,
                PurchaseDate = request.PurchaseDate,
                PurchasePrice = request.PurchasePrice,
                Vendor = request.Vendor,
                Location = request.Location,
                Specs = request.Specs
            };

            var created = await _assetService.CreateAsync(asset);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create asset");
            return StatusCode(500, new { error = "Failed to create asset" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateAssetRequest request)
    {
        try
        {
            var asset = new Asset
            {
                AssetName = request.AssetName,
                Category = request.Category,
                Status = request.Status,
                Location = request.Location,
                Specs = request.Specs,
                PurchasePrice = request.PurchasePrice,
                DepartmentId = request.DepartmentId,
                UserId = request.UserId
            };

            await _assetService.UpdateAsync(id, asset);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await _assetService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetStatistics()
    {
        var stats = await _assetService.GetStatisticsAsync();
        return Ok(stats);
    }
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CreateAssetRequest
{
    [Required] public string AssetCode { get; set; } = "";
    [Required] public string AssetName { get; set; } = "";
    public string? AssetType { get; set; }
    public string? Category { get; set; }
    public string? DepartmentId { get; set; }
    public string? UserId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Vendor { get; set; }
    public string? Location { get; set; }
    public string? Specs { get; set; }
}

public class UpdateAssetRequest
{
    public string? AssetName { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
    public string? Specs { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? DepartmentId { get; set; }
    public string? UserId { get; set; }
}
