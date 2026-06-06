using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Interfaces;
using Microsoft.Win32;

namespace AssetManagementSystem.Desktop.ViewModels;

public partial class ReportViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly IAssetService _assetService;

    [ObservableProperty]
    private List<AssetStatusStatDto> _statusStats = new();

    [ObservableProperty]
    private List<DepartmentStatDto> _departmentStats = new();

    [ObservableProperty]
    private List<CategoryStatDto> _categoryStats = new();

    [ObservableProperty]
    private bool _isLoading;

    public ReportViewModel()
    {
        _reportService = App.CurrentApp.ReportService;
        _assetService = App.CurrentApp.AssetService;
        _ = LoadReportsAsync();
    }

    [RelayCommand]
    private async Task LoadReportsAsync()
    {
        IsLoading = true;
        StatusStats = await _reportService.GetAssetStatusStatsAsync();
        DepartmentStats = await _reportService.GetDepartmentStatsAsync();
        CategoryStats = await _reportService.GetCategoryStatsAsync();
        IsLoading = false;
    }

    [RelayCommand]
    private async Task ExportDepartmentReportAsync()
    {
        var dialog = new SaveFileDialog { Filter = "Excel files (*.xlsx)|*.xlsx", FileName = "部门统计报表.xlsx" };
        if (dialog.ShowDialog() != true) return;

        var bytes = await _reportService.ExportToExcelAsync(DepartmentStats, "部门统计");
        await System.IO.File.WriteAllBytesAsync(dialog.FileName, bytes);
        System.Windows.MessageBox.Show("导出成功！", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
