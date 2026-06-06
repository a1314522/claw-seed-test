using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Interfaces;
using Microsoft.Win32;

namespace AssetManagementSystem.Desktop.ViewModels;

public partial class AssetListViewModel : ObservableObject
{
    private readonly IAssetService _assetService;
    private readonly IReportService _reportService;

    [ObservableProperty]
    private List<AssetDto> _assets = new();

    [ObservableProperty]
    private AssetListResultDto _queryResult = new();

    [ObservableProperty]
    private AssetQueryDto _query = new();

    [ObservableProperty]
    private AssetDto? _selectedAsset;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public AssetListViewModel()
    {
        _assetService = App.CurrentApp.AssetService;
        _reportService = App.CurrentApp.ReportService;
        _ = LoadAssetsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        Query.Keyword = value;
    }

    [RelayCommand]
    private async Task LoadAssetsAsync()
    {
        IsLoading = true;
        Query.Keyword = SearchText;
        QueryResult = await _assetService.GetAssetsAsync(Query);
        Assets = QueryResult.Items;
        IsLoading = false;
    }

    [RelayCommand]
    private async Task Search()
    {
        Query.Page = 1;
        await LoadAssetsAsync();
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (Query.Page < QueryResult.TotalPages)
        {
            Query.Page++;
            await LoadAssetsAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (Query.Page > 1)
        {
            Query.Page--;
            await LoadAssetsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteAsset(AssetDto? asset)
    {
        var target = asset ?? SelectedAsset;
        if (target == null) return;
        var result = System.Windows.MessageBox.Show(
            $"确认删除资产 {target.Name} ({target.AssetCode})？",
            "删除确认", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        await _assetService.DeleteAssetAsync(target.Id);
        await LoadAssetsAsync();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var dialog = new SaveFileDialog { Filter = "Excel files (*.xlsx)|*.xlsx", FileName = "资产清单.xlsx" };
        if (dialog.ShowDialog() != true) return;

        var data = await _assetService.ExportAsync(Query);
        var bytes = await _reportService.ExportToExcelAsync(data, "资产清单");
        await System.IO.File.WriteAllBytesAsync(dialog.FileName, bytes);
        System.Windows.MessageBox.Show("导出成功！", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var dialog = new OpenFileDialog { Filter = "Excel files (*.xlsx)|*.xlsx" };
        if (dialog.ShowDialog() != true) return;

        System.Windows.MessageBox.Show("导入功能暂未实现", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    private void AddAsset()
    {
        var nav = App.CurrentApp.NavigationService;
        nav.NavigateTo(new Views.AssetEditPage());
    }

    [RelayCommand]
    private void EditAsset(AssetDto? asset)
    {
        var target = asset ?? SelectedAsset;
        if (target == null) return;
        var nav = App.CurrentApp.NavigationService;
        nav.NavigateTo(new Views.AssetEditPage(target.Id));
    }
}
