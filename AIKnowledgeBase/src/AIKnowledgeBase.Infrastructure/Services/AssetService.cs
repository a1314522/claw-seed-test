using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeBase.Infrastructure.Services;

public class AssetService : IAssetService
{
    private readonly ApplicationDbContext _context;
    private readonly IMeiliSearchService _searchService;

    public AssetService(ApplicationDbContext context, IMeiliSearchService searchService)
    {
        _context = context;
        _searchService = searchService;
    }

    public async Task<Asset?> GetByIdAsync(Guid id)
    {
        return await _context.Assets
            .Include(a => a.Department)
            .Include(a => a.Logs)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsDeleted != true);
    }

    public async Task<IEnumerable<Asset>> GetAllAsync(string? category = null, string? status = null, int page = 1, int pageSize = 50)
    {
        var query = _context.Assets
            .Where(a => a.IsDeleted != true)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(a => a.Category == category);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        return await query
            .Include(a => a.Department)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Asset> CreateAsync(Asset asset)
    {
        asset.Id = Guid.NewGuid();
        asset.CreatedAt = DateTime.UtcNow;
        asset.Status = "in_use";

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        // Index in MeiliSearch
        await _searchService.IndexAssetAsync(asset);

        // Add log
        _context.AssetLogs.Add(new AssetLog
        {
            Id = Guid.NewGuid(),
            AssetId = asset.Id,
            Action = "created",
            Description = $"资产 {asset.AssetCode} 已创建",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = asset.CreatedBy
        });
        await _context.SaveChangesAsync();

        return asset;
    }

    public async Task<Asset> UpdateAsync(Guid id, Asset asset)
    {
        var existing = await _context.Assets.FindAsync(id);
        if (existing == null || existing.IsDeleted == true)
            throw new KeyNotFoundException("Asset not found");

        existing.AssetName = asset.AssetName;
        existing.Category = asset.Category;
        existing.Status = asset.Status;
        existing.Location = asset.Location;
        existing.Specs = asset.Specs;
        existing.PurchasePrice = asset.PurchasePrice;
        existing.DepartmentId = asset.DepartmentId;
        existing.UserId = asset.UserId;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _searchService.UpdateAssetAsync(existing);

        return existing;
    }

    public async Task DeleteAsync(Guid id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null) throw new KeyNotFoundException("Asset not found");

        asset.IsDeleted = true;
        asset.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _searchService.DeleteAssetAsync(id);
    }

    public async Task<Dictionary<string, int>> GetStatisticsAsync()
    {
        var total = await _context.Assets.CountAsync(a => a.IsDeleted != true);
        var inUse = await _context.Assets.CountAsync(a => a.Status == "in_use" && a.IsDeleted != true);
        var maintenance = await _context.Assets.CountAsync(a => a.Status == "maintenance" && a.IsDeleted != true);
        var scrap = await _context.Assets.CountAsync(a => a.Status == "scrap" && a.IsDeleted != true);

        return new Dictionary<string, int>
        {
            ["total"] = total,
            ["inUse"] = inUse,
            ["maintenance"] = maintenance,
            ["scrap"] = scrap
        };
    }
}

public interface IAssetService
{
    Task<Asset?> GetByIdAsync(Guid id);
    Task<IEnumerable<Asset>> GetAllAsync(string? category = null, string? status = null, int page = 1, int pageSize = 50);
    Task<Asset> CreateAsync(Asset asset);
    Task<Asset> UpdateAsync(Guid id, Asset asset);
    Task DeleteAsync(Guid id);
    Task<Dictionary<string, int>> GetStatisticsAsync();
}
