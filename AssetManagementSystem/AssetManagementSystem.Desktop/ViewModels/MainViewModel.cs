using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AssetManagementSystem.Desktop.Services;

namespace AssetManagementSystem.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    private string _currentPageTitle = "首页";

    [ObservableProperty]
    private bool _isMenuOpen = true;

    [ObservableProperty]
    private object? _currentPage;

    public MainViewModel(NavigationService navigationService)
    {
        _navigationService = navigationService;
        NavigateToDashboard();
    }

    [RelayCommand]
    public void NavigateToDashboard()
    {
        CurrentPageTitle = "首页";
        CurrentPage = new Views.DashboardPage();
    }

    [RelayCommand]
    public void NavigateToAssets()
    {
        CurrentPageTitle = "资产卡片";
        CurrentPage = new Views.AssetListPage();
    }

    [RelayCommand]
    public void NavigateToCategories()
    {
        CurrentPageTitle = "资产分类";
        CurrentPage = new Views.CategoryListPage();
    }

    [RelayCommand]
    public void NavigateToPurchases()
    {
        CurrentPageTitle = "采购入账";
        CurrentPage = new Views.PurchaseListPage();
    }

    [RelayCommand]
    public void NavigateToConsumables()
    {
        CurrentPageTitle = "消耗品管理";
        CurrentPage = new Views.ConsumableListPage();
    }

    [RelayCommand]
    public void NavigateToReports()
    {
        CurrentPageTitle = "报表统计";
        CurrentPage = new Views.ReportPage();
    }

    [RelayCommand]
    public void NavigateToSettings()
    {
        CurrentPageTitle = "系统设置";
        CurrentPage = new Views.SettingsPage();
    }

    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;
    }
}
