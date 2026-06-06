using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Interfaces;
using AssetManagementSystem.Desktop.Views;

namespace AssetManagementSystem.Desktop.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly IAssetService _assetService;

    [ObservableProperty]
    private DashboardStatisticsDto _statistics = new();

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private List<AssetDto> _recentAssets = new();

    public DashboardViewModel()
    {
        _reportService = App.CurrentApp.ReportService;
        _assetService = App.CurrentApp.AssetService;
        _ = LoadDataAsync();
    }

    public int TotalAssets => Statistics.TotalAssets;
    public int InUseAssets => Statistics.ActiveAssets;
    public int RepairingAssets => Statistics.InRepairAssets;
    public int MonthlyPurchases => Statistics.PendingPurchases;

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            Statistics = await _reportService.GetDashboardStatisticsAsync();
            OnPropertyChanged(nameof(TotalAssets));
            OnPropertyChanged(nameof(InUseAssets));
            OnPropertyChanged(nameof(RepairingAssets));
            OnPropertyChanged(nameof(MonthlyPurchases));
            var result = await _assetService.GetAssetsAsync(new Core.Dtos.AssetQueryDto { Page = 1, PageSize = 10 });
            RecentAssets = result.Items;
            IsLoading = false;
        }
        catch
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateToAssets()
    {
        var nav = App.CurrentApp.NavigationService;
        nav.NavigateTo(new AssetListPage());
    }

    [RelayCommand]
    private void NavigateToPurchases()
    {
        var nav = App.CurrentApp.NavigationService;
        nav.NavigateTo(new PurchaseListPage());
    }

    [RelayCommand]
    private void NavigateToConsumables()
    {
        var nav = App.CurrentApp.NavigationService;
        nav.NavigateTo(new ConsumableListPage());
    }
}
