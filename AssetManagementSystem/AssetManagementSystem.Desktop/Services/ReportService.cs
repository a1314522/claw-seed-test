using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Interfaces;
using AssetManagementSystem.Desktop.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementSystem.Desktop.Services;

public class ReportService(AppDbContext context) : IReportService
{
    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync()
    {
        var totalAssets = await context.Assets.CountAsync();
        var totalCategories = await context.AssetCategories.CountAsync();
        var totalConsumables = await context.Consumables.CountAsync();
        var activeAssets = await context.Assets.CountAsync(a => a.Status == Core.Enums.AssetStatus.在用);
        var inRepair = await context.Assets.CountAsync(a => a.Status == Core.Enums.AssetStatus.维修);
        var scrap = await context.Assets.CountAsync(a => a.Status == Core.Enums.AssetStatus.报废);
        var pendingPurchases = await context.Purchases.CountAsync(p => p.Status == Core.Enums.PurchaseStatus.审批中);
        var lowStock = await context.Consumables.CountAsync(c => c.StockQuantity <= c.MinStock);
        var totalValue = await context.Assets.SumAsync(a => (decimal?)a.PurchasePrice) ?? 0m;

        var statusStats = await context.Assets
            .GroupBy(a => a.Status)
            .Select(g => new AssetStatusStatDto { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var deptStats = await context.Assets
            .Where(a => a.Department != null)
            .GroupBy(a => a.Department!)
            .Select(g => new DepartmentStatDto { Department = g.Key, Count = g.Count(), TotalValue = g.Sum(a => a.PurchasePrice) })
            .ToListAsync();

        var catStats = await context.Assets
            .GroupBy(a => a.Category!.Name)
            .Select(g => new CategoryStatDto { CategoryName = g.Key, Count = g.Count() })
            .ToListAsync();

        return new DashboardStatisticsDto
        {
            TotalAssets = totalAssets,
            TotalCategories = totalCategories,
            TotalConsumables = totalConsumables,
            ActiveAssets = activeAssets,
            InRepairAssets = inRepair,
            ScrapAssets = scrap,
            PendingPurchases = pendingPurchases,
            LowStockConsumables = lowStock,
            TotalAssetValue = totalValue,
            StatusStats = statusStats,
            DepartmentStats = deptStats,
            CategoryStats = catStats
        };
    }

    public async Task<List<AssetStatusStatDto>> GetAssetStatusStatsAsync()
    {
        return await context.Assets
            .GroupBy(a => a.Status)
            .Select(g => new AssetStatusStatDto { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
    }

    public async Task<List<DepartmentStatDto>> GetDepartmentStatsAsync()
    {
        return await context.Assets
            .Where(a => a.Department != null)
            .GroupBy(a => a.Department!)
            .Select(g => new DepartmentStatDto { Department = g.Key, Count = g.Count(), TotalValue = g.Sum(a => a.PurchasePrice) })
            .ToListAsync();
    }

    public async Task<List<CategoryStatDto>> GetCategoryStatsAsync()
    {
        return await context.Assets
            .GroupBy(a => a.Category!.Name)
            .Select(g => new CategoryStatDto { CategoryName = g.Key, Count = g.Count() })
            .ToListAsync();
    }

    public Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName)
    {
        using var package = new OfficeOpenXml.ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add(sheetName);

        var props = typeof(T).GetProperties().Where(p => p.CanRead).ToArray();
        for (int i = 0; i < props.Length; i++)
            sheet.Cells[1, i + 1].Value = props[i].Name;

        for (int row = 0; row < data.Count; row++)
        {
            for (int col = 0; col < props.Length; col++)
            {
                var val = props[col].GetValue(data[row]);
                sheet.Cells[row + 2, col + 1].Value = val;
            }
        }

        sheet.Cells[1, 1, 1, props.Length].Style.Font.Bold = true;
        sheet.Cells.AutoFitColumns();

        return Task.FromResult(package.GetAsByteArray());
    }
}
