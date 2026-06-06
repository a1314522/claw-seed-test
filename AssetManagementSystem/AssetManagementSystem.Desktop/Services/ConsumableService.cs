using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Interfaces;
using AssetManagementSystem.Core.Models;
using AssetManagementSystem.Desktop.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementSystem.Desktop.Services;

public class ConsumableService(AppDbContext context) : IConsumableService
{
    public async Task<List<ConsumableDto>> GetConsumablesAsync(string? keyword = null)
    {
        var q = context.Consumables.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(c => c.Name.Contains(keyword) || (c.Description != null && c.Description.Contains(keyword)));
        return await q.OrderBy(c => c.Name)
            .Select(c => MapToDto(c)).ToListAsync();
    }

    public async Task<ConsumableDto?> GetConsumableByIdAsync(int id)
    {
        var c = await context.Consumables.FindAsync(id);
        return c == null ? null : MapToDto(c);
    }

    public async Task<ConsumableDto> CreateConsumableAsync(ConsumableDto dto)
    {
        var c = new Consumable
        {
            Name = dto.Name,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            Specification = dto.Specification,
            Unit = dto.Unit,
            StockQuantity = dto.StockQuantity,
            MinStock = dto.MinStock,
            StorageLocation = dto.StorageLocation,
            Supplier = dto.Supplier,
            UnitPrice = dto.UnitPrice,
            Notes = dto.Notes
        };
        context.Consumables.Add(c);
        await context.SaveChangesAsync();
        return MapToDto(c);
    }

    public async Task<ConsumableDto> UpdateConsumableAsync(int id, ConsumableDto dto)
    {
        var c = await context.Consumables.FindAsync(id) ?? throw new Exception($"消耗品 ID {id} 不存在");
        c.Name = dto.Name;
        c.Description = dto.Description;
        c.CategoryId = dto.CategoryId;
        c.Specification = dto.Specification;
        c.Unit = dto.Unit;
        c.StockQuantity = dto.StockQuantity;
        c.MinStock = dto.MinStock;
        c.StorageLocation = dto.StorageLocation;
        c.Supplier = dto.Supplier;
        c.UnitPrice = dto.UnitPrice;
        c.Notes = dto.Notes;
        c.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        return MapToDto(c);
    }

    public async Task<bool> DeleteConsumableAsync(int id)
    {
        var c = await context.Consumables.FindAsync(id);
        if (c == null) return false;
        context.Consumables.Remove(c);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<ConsumableUsageDto> RecordUsageAsync(CreateConsumableUsageDto dto)
    {
        var consumable = await context.Consumables.FindAsync(dto.ConsumableId)
            ?? throw new Exception("消耗品不存在");
        if (consumable.StockQuantity < dto.Quantity)
            throw new Exception($"库存不足，当前库存：{consumable.StockQuantity}");

        consumable.StockQuantity -= dto.Quantity;
        consumable.UpdatedAt = DateTime.Now;

        var usage = new ConsumableUsage
        {
            ConsumableId = dto.ConsumableId,
            Quantity = dto.Quantity,
            User = dto.User,
            Department = dto.Department,
            Purpose = dto.Purpose,
            Notes = dto.Notes
        };
        context.ConsumableUsages.Add(usage);
        await context.SaveChangesAsync();
        return new ConsumableUsageDto
        {
            Id = usage.Id,
            ConsumableId = usage.ConsumableId,
            ConsumableName = consumable.Name,
            Quantity = usage.Quantity,
            User = usage.User,
            Department = usage.Department,
            Purpose = usage.Purpose,
            UsageDate = usage.UsageDate,
            Notes = usage.Notes
        };
    }

    public async Task<List<ConsumableUsageDto>> GetUsagesAsync(int consumableId)
    {
        return await context.ConsumableUsages
            .Where(u => u.ConsumableId == consumableId)
            .OrderByDescending(u => u.UsageDate)
            .Select(u => new ConsumableUsageDto
            {
                Id = u.Id,
                ConsumableId = u.ConsumableId,
                ConsumableName = u.Consumable!.Name,
                Quantity = u.Quantity,
                User = u.User,
                Department = u.Department,
                Purpose = u.Purpose,
                UsageDate = u.UsageDate,
                Notes = u.Notes
            }).ToListAsync();
    }

    public async Task<List<ConsumableDto>> GetLowStockConsumablesAsync()
    {
        return await context.Consumables
            .Where(c => c.StockQuantity <= c.MinStock)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    private static ConsumableDto MapToDto(Consumable c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        CategoryId = c.CategoryId,
        Specification = c.Specification,
        Unit = c.Unit,
        StockQuantity = c.StockQuantity,
        MinStock = c.MinStock,
        StorageLocation = c.StorageLocation,
        Supplier = c.Supplier,
        UnitPrice = c.UnitPrice,
        Notes = c.Notes,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
