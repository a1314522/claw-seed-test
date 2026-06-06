using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Enums;
using AssetManagementSystem.Core.Interfaces;
using AssetManagementSystem.Core.Models;
using AssetManagementSystem.Desktop.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementSystem.Desktop.Services;

public class PurchaseService(AppDbContext context) : IPurchaseService
{
    public async Task<List<PurchaseDto>> GetPurchasesAsync(string? status = null)
    {
        var q = context.Purchases.Include(p => p.Items).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(p => p.Status.ToString() == status);
        return await q.OrderByDescending(p => p.CreatedAt)
            .Select(p => MapToDto(p)).ToListAsync();
    }

    public async Task<PurchaseDto?> GetPurchaseByIdAsync(int id)
    {
        var p = await context.Purchases.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        return p == null ? null : MapToDto(p);
    }

    public async Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseDto dto)
    {
        var purchase = new Purchase
        {
            PurchaseNo = dto.PurchaseNo,
            Name = dto.Name,
            Description = dto.Description,
            Applicant = dto.Applicant,
            Department = dto.Department,
            TotalAmount = dto.TotalAmount,
            Quantity = dto.Quantity,
            Supplier = dto.Supplier,
            ApplyDate = dto.ApplyDate,
            Notes = dto.Notes
        };
        context.Purchases.Add(purchase);
        await context.SaveChangesAsync();

        foreach (var item in dto.Items)
        {
            context.PurchaseItems.Add(new PurchaseItem
            {
                PurchaseId = purchase.Id,
                Name = item.Name,
                CategoryId = item.CategoryId,
                Brand = item.Brand,
                Model = item.Model,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice,
                Supplier = item.Supplier,
                Notes = item.Notes
            });
        }
        await context.SaveChangesAsync();
        return MapToDto(purchase);
    }

    public async Task<PurchaseDto> ApprovePurchaseAsync(int id, string approver)
    {
        var p = await context.Purchases.FindAsync(id) ?? throw new Exception($"采购单 ID {id} 不存在");
        p.Status = PurchaseStatus.已审批;
        p.Approver = approver;
        p.ApproveDate = DateTime.Now;
        p.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        return MapToDto(p);
    }

    public async Task<PurchaseDto> RejectPurchaseAsync(int id, string reason)
    {
        var p = await context.Purchases.FindAsync(id) ?? throw new Exception($"采购单 ID {id} 不存在");
        p.Status = PurchaseStatus.已取消;
        p.RejectReason = reason;
        p.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        return MapToDto(p);
    }

    public async Task<PurchaseDto> CompletePurchaseAsync(int id)
    {
        var p = await context.Purchases.FindAsync(id) ?? throw new Exception($"采购单 ID {id} 不存在");
        p.Status = PurchaseStatus.已采购;
        p.PurchaseDate = DateTime.Now;
        p.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        return MapToDto(p);
    }

    public async Task<PurchaseDto> ReceivePurchaseAsync(int id)
    {
        var p = await context.Purchases.FindAsync(id) ?? throw new Exception($"采购单 ID {id} 不存在");
        p.Status = PurchaseStatus.已入库;
        p.ReceiveDate = DateTime.Now;
        p.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        return MapToDto(p);
    }

    public async Task<bool> DeletePurchaseAsync(int id)
    {
        var p = await context.Purchases.FindAsync(id);
        if (p == null) return false;
        context.Purchases.Remove(p);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<PurchaseDto> GenerateAssetsFromPurchaseAsync(int id)
    {
        var p = await context.Purchases.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new Exception($"采购单 ID {id} 不存在");
        if (p.Status != PurchaseStatus.已入库)
            throw new Exception("只有已入库的采购单才能生成资产卡片");

        foreach (var item in p.Items)
        {
            for (int i = 0; i < item.Quantity; i++)
            {
                var code = $"{p.PurchaseNo}-{item.Id}-{i + 1:D3}";
                var asset = new Asset
                {
                    AssetCode = code,
                    Name = item.Name,
                    CategoryId = item.CategoryId,
                    Brand = item.Brand,
                    Model = item.Model,
                    PurchasePrice = item.UnitPrice,
                    PurchaseDate = p.PurchaseDate,
                    Supplier = item.Supplier ?? p.Supplier,
                    PurchaseOrderNo = p.PurchaseNo,
                    PurchaseId = p.Id,
                    Status = AssetStatus.入库,
                    Department = p.Department
                };
                context.Assets.Add(asset);
            }
        }
        await context.SaveChangesAsync();
        return MapToDto(p);
    }

    private static PurchaseDto MapToDto(Purchase p) => new()
    {
        Id = p.Id,
        PurchaseNo = p.PurchaseNo,
        Name = p.Name,
        Description = p.Description,
        Applicant = p.Applicant,
        Department = p.Department,
        TotalAmount = p.TotalAmount,
        Quantity = p.Quantity,
        Supplier = p.Supplier,
        ApplyDate = p.ApplyDate,
        ApproveDate = p.ApproveDate,
        Approver = p.Approver,
        PurchaseDate = p.PurchaseDate,
        ReceiveDate = p.ReceiveDate,
        Status = p.Status.ToString(),
        RejectReason = p.RejectReason,
        Notes = p.Notes,
        CreatedAt = p.CreatedAt,
        Items = p.Items.Select(i => new PurchaseItemDto
        {
            Id = i.Id,
            Name = i.Name,
            CategoryId = i.CategoryId,
            Brand = i.Brand,
            Model = i.Model,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice,
            Supplier = i.Supplier,
            Notes = i.Notes
        }).ToList()
    };
}
