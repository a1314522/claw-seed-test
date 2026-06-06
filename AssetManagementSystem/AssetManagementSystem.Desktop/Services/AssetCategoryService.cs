using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Interfaces;
using AssetManagementSystem.Core.Models;
using AssetManagementSystem.Desktop.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementSystem.Desktop.Services;

public class AssetCategoryService(AppDbContext context) : IAssetCategoryService
{
    public async Task<List<AssetCategoryDto>> GetCategoriesAsync()
    {
        var all = await context.AssetCategories.ToListAsync();
        return all.Where(c => c.ParentId == null)
            .Select(c => MapToDto(c, all))
            .ToList();
    }

    public async Task<AssetCategoryDto?> GetCategoryByIdAsync(int id)
    {
        var all = await context.AssetCategories.ToListAsync();
        var cat = all.FirstOrDefault(c => c.Id == id);
        return cat == null ? null : MapToDto(cat, all);
    }

    public async Task<AssetCategoryDto> CreateCategoryAsync(CreateAssetCategoryDto dto)
    {
        var cat = new AssetCategory
        {
            Name = dto.Name,
            Description = dto.Description,
            ParentId = dto.ParentId,
            SortOrder = dto.SortOrder
        };
        context.AssetCategories.Add(cat);
        await context.SaveChangesAsync();
        return new AssetCategoryDto { Id = cat.Id, Name = cat.Name, Description = cat.Description, ParentId = cat.ParentId, SortOrder = cat.SortOrder };
    }

    public async Task<AssetCategoryDto> UpdateCategoryAsync(int id, CreateAssetCategoryDto dto)
    {
        var cat = await context.AssetCategories.FindAsync(id) ?? throw new Exception($"分类 ID {id} 不存在");
        cat.Name = dto.Name;
        cat.Description = dto.Description;
        cat.ParentId = dto.ParentId;
        cat.SortOrder = dto.SortOrder;
        cat.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        return new AssetCategoryDto { Id = cat.Id, Name = cat.Name, Description = cat.Description, ParentId = cat.ParentId, SortOrder = cat.SortOrder };
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var cat = await context.AssetCategories.FindAsync(id);
        if (cat == null) return false;
        var hasChildren = await context.AssetCategories.AnyAsync(c => c.ParentId == id);
        if (hasChildren) throw new Exception("该分类下存在子分类，无法删除");
        var hasAssets = await context.Assets.AnyAsync(a => a.CategoryId == id);
        if (hasAssets) throw new Exception("该分类下存在资产，无法删除");
        context.AssetCategories.Remove(cat);
        await context.SaveChangesAsync();
        return true;
    }

    private AssetCategoryDto MapToDto(AssetCategory c, List<AssetCategory> all)
    {
        var children = all.Where(x => x.ParentId == c.Id).Select(x => MapToDto(x, all)).ToList();
        return new AssetCategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ParentId = c.ParentId,
            SortOrder = c.SortOrder,
            Children = children,
            AssetCount = all.Count(x => x.Id == c.Id)
        };
    }
}
