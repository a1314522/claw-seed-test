using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeBase.API.Controllers
{
    [ApiController]
    [Route("api/v1/am/[controller]")]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AssetsController> _logger;

        public AssetsController(ApplicationDbContext context, ILogger<AssetsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssets([FromQuery] string? category = null, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.Assets.AsNoTracking().AsQueryable();
            
            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category == category);
            
            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);
            
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return Ok(new { total, page, pageSize, items });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Asset>> GetAsset(Guid id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound();
            return Ok(asset);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AssetManager")]
        public async Task<ActionResult<Asset>> CreateAsset(Asset asset)
        {
            asset.Id = Guid.NewGuid();
            asset.CreatedAt = DateTime.UtcNow;
            asset.Status = "in_use";
            
            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Asset created: {AssetCode}", asset.AssetCode);
            
            return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, asset);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,AssetManager")]
        public async Task<IActionResult> UpdateAsset(Guid id, Asset asset)
        {
            if (id != asset.Id) return BadRequest();
            
            _context.Entry(asset).State = EntityState.Modified;
            asset.UpdatedAt = DateTime.UtcNow;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssetExists(id)) return NotFound();
                throw;
            }
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAsset(Guid id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound();
            
            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        [HttpPost("{id}/transfer")]
        [Authorize(Roles = "Admin,AssetManager")]
        public async Task<IActionResult> TransferAsset(Guid id, [FromBody] TransferRequest request)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound();
            
            // Log transfer
            var log = new AssetLog
            {
                Id = Guid.NewGuid(),
                AssetId = id,
                ActionType = "transfer",
                FromDepartmentId = asset.DepartmentId,
                ToDepartmentId = request.DepartmentId,
                FromUserId = asset.UserId,
                ToUserId = request.UserId,
                ActionDate = DateTime.UtcNow,
                Remark = request.Remark,
                OperatedBy = User.Identity?.Name
            };
            
            _context.AssetLogs.Add(log);
            
            // Update asset
            asset.DepartmentId = request.DepartmentId;
            asset.UserId = request.UserId;
            asset.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Asset transferred successfully" });
        }

        [HttpPost("{id}/scrap")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ScrapAsset(Guid id, [FromBody] ScrapRequest request)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound();
            
            asset.Status = "scrapped";
            asset.ScrapDate = DateTime.UtcNow;
            asset.ScrapReason = request.Reason;
            asset.UpdatedAt = DateTime.UtcNow;
            
            // Log scrap
            var log = new AssetLog
            {
                Id = Guid.NewGuid(),
                AssetId = id,
                ActionType = "scrap",
                ActionDate = DateTime.UtcNow,
                Remark = request.Reason,
                OperatedBy = User.Identity?.Name
            };
            
            _context.AssetLogs.Add(log);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Asset scrapped successfully" });
        }

        [HttpGet("reports/summary")]
        public async Task<ActionResult> GetSummary()
        {
            var total = await _context.Assets.CountAsync();
            var byCategory = await _context.Assets
                .GroupBy(a => a.Category)
                .Select(g => new { category = g.Key, count = g.Count() })
                .ToListAsync();
            
            var byStatus = await _context.Assets
                .GroupBy(a => a.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();
            
            return Ok(new { total, byCategory, byStatus });
        }

        private bool AssetExists(Guid id)
        {
            return _context.Assets.Any(e => e.Id == id);
        }
    }

    public class TransferRequest
    {
        public Guid DepartmentId { get; set; }
        public Guid? UserId { get; set; }
        public string? Remark { get; set; }
    }

    public class ScrapRequest
    {
        public string Reason { get; set; } = "";
    }
}
