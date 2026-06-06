using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Enums;
using AssetManagementSystem.Core.Interfaces;
using AssetManagementSystem.Core.Models;
using AssetManagementSystem.Desktop.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementSystem.Desktop.Services;

public class AssetService(AppDbContext context) : IAssetService
{
    public async Task<AssetListResultDto> GetAssetsAsync(AssetQueryDto query)
    {
        var q = context.Assets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(a => a.Name.Contains(query.Keyword) || a.AssetCode.Contains(query.Keyword) || a.SerialNumber != null && a.SerialNumber.Contains(query.Keyword));

        if (query.CategoryId.HasValue)
            q = q.Where(a => a.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(a => a.Status.ToString() == query.Status);

        if (!string.IsNullOrWhiteSpace(query.Department))
            q = q.Where(a => a.Department == query.Department);

        if (!string.IsNullOrWhiteSpace(query.Location))
            q = q.Where(a => a.Location != null && a.Location.Contains(query.Location));

        if (query.PurchaseDateFrom.HasValue)
            q = q.Where(a => a.PurchaseDate >= query.PurchaseDateFrom.Value);

        if (query.PurchaseDateTo.HasValue)
            q = q.Where(a => a.PurchaseDate <= query.PurchaseDateTo.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();

        return new AssetListResultDto
        {
            Items = items,
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
        };
    }

    public async Task<AssetDto?> GetAssetByIdAsync(int id)
    {
        var asset = await context.Assets.FindAsync(id);
        return asset == null ? null : MapToDto(asset);
    }

    public async Task<AssetDto?> GetAssetByCodeAsync(string code)
    {
        var asset = await context.Assets.FirstOrDefaultAsync(a => a.AssetCode == code);
        return asset == null ? null : MapToDto(asset);
    }

    public async Task<AssetDto> CreateAssetAsync(CreateAssetDto dto)
    {
        var asset = new Asset
        {
            AssetCode = dto.AssetCode,
            Name = dto.Name,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            Brand = dto.Brand,
            Model = dto.Model,
            SerialNumber = dto.SerialNumber,
            Location = dto.Location,
            Department = dto.Department,
            Owner = dto.Owner,
            AdUserName = dto.AdUserName,
            PurchasePrice = dto.PurchasePrice,
            PurchaseDate = dto.PurchaseDate,
            Supplier = dto.Supplier,
            PurchaseOrderNo = dto.PurchaseOrderNo,
            PurchaseId = dto.PurchaseId,
            WarrantyPeriod = dto.WarrantyPeriod,
            WarrantyExpireDate = dto.WarrantyExpireDate,
            Notes = dto.Notes,
            Status = Enum.TryParse<AssetStatus>(dto.Status, out var status) ? status : AssetStatus.入库
        };
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
        return MapToDto(asset);
    }

    public async Task<AssetDto> UpdateAssetAsync(int id, UpdateAssetDto dto)
    {
        var asset = await context.Assets.FindAsync(id) ?? throw new Exception($"资产 ID {id} 不存在");
        asset.AssetCode = dto.AssetCode;
        asset.Name = dto.Name;
        asset.Description = dto.Description;
        asset.CategoryId = dto.CategoryId;
        asset.Brand = dto.Brand;
        asset.Model = dto.Model;
        asset.SerialNumber = dto.SerialNumber;
        asset.Location = dto.Location;
        asset.Department = dto.Department;
        asset.Owner = dto.Owner;
        asset.AdUserName = dto.AdUserName;
        asset.PurchasePrice = dto.PurchasePrice;
        asset.PurchaseDate = dto.PurchaseDate;
        asset.Supplier = dto.Supplier;
        asset.PurchaseOrderNo = dto.PurchaseOrderNo;
        asset.PurchaseId = dto.PurchaseId;
        asset.WarrantyPeriod = dto.WarrantyPeriod;
        asset.WarrantyExpireDate = dto.WarrantyExpireDate;
        asset.Notes = dto.Notes;
        if (Enum.TryParse<AssetStatus>(dto.Status, out var status))
            asset.Status = status;
        asset.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        return MapToDto(asset);
    }

    public async Task<bool> DeleteAssetAsync(int id)
    {
        var asset = await context.Assets.FindAsync(id);
        if (asset == null) return false;
        context.Assets.Remove(asset);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ScrapAssetAsync(int id, string reason)
    {
        var asset = await context.Assets.FindAsync(id);
        if (asset == null) return false;
        asset.Status = AssetStatus.报废;
        asset.ScrapDate = DateTime.Now;
        asset.ScrapReason = reason;
        asset.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangeAssetStatusAsync(int id, AssetStatus status)
    {
        var asset = await context.Assets.FindAsync(id);
        if (asset == null) return false;
        asset.Status = status;
        asset.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<AssetDto>> GetAssetsByPurchaseIdAsync(int purchaseId)
    {
        return await context.Assets.Where(a => a.PurchaseId == purchaseId)
            .Select(a => MapToDto(a)).ToListAsync();
    }

    public async Task<bool> BatchImportAsync(List<CreateAssetDto> assets)
    {
        foreach (var dto in assets)
        {
            var asset = new Asset
            {
                AssetCode = dto.AssetCode,
                Name = dto.Name,
                CategoryId = dto.CategoryId,
                Brand = dto.Brand,
                Model = dto.Model,
                SerialNumber = dto.SerialNumber,
                Location = dto.Location,
                Department = dto.Department,
                Owner = dto.Owner,
                AdUserName = dto.AdUserName,
                PurchasePrice = dto.PurchasePrice,
                PurchaseDate = dto.PurchaseDate,
                Supplier = dto.Supplier,
                PurchaseOrderNo = dto.PurchaseOrderNo,
                WarrantyPeriod = dto.WarrantyPeriod,
                WarrantyExpireDate = dto.WarrantyExpireDate,
                Notes = dto.Notes
            };
            context.Assets.Add(asset);
        }
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<AssetDto>> ExportAsync(AssetQueryDto query)
    {
        var q = context.Assets.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(a => a.Name.Contains(query.Keyword) || a.AssetCode.Contains(query.Keyword));
        if (query.CategoryId.HasValue)
            q = q.Where(a => a.CategoryId == query.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(a => a.Status.ToString() == query.Status);
        return await q.Select(a => MapToDto(a)).ToListAsync();
    }

    private static AssetDto MapToDto(Asset a) => new()
    {
        Id = a.Id,
        AssetCode = a.AssetCode,
        Name = a.Name,
        Description = a.Description,
        CategoryId = a.CategoryId,
        Brand = a.Brand,
        Model = a.Model,
        SerialNumber = a.SerialNumber,
        Location = a.Location,
        Department = a.Department,
        Owner = a.Owner,
        AdUserName = a.AdUserName,
        PurchasePrice = a.PurchasePrice,
        PurchaseDate = a.PurchaseDate,
        Supplier = a.Supplier,
        PurchaseOrderNo = a.PurchaseOrderNo,
        Status = a.Status.ToString(),
        PurchaseId = a.PurchaseId,
        SyncStatus = a.SyncStatus.ToString(),
        KingdeeId = a.KingdeeId,
        WarrantyPeriod = a.WarrantyPeriod,
        WarrantyExpireDate = a.WarrantyExpireDate,
        Notes = a.Notes,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
        ScrapDate = a.ScrapDate,
        ScrapReason = a.ScrapReason
    };
}
