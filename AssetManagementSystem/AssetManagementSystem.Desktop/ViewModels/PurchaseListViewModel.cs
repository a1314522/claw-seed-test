using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Enums;
using AssetManagementSystem.Core.Interfaces;

namespace AssetManagementSystem.Desktop.ViewModels;

public partial class PurchaseListViewModel : ObservableObject
{
    private readonly IPurchaseService _purchaseService;

    [ObservableProperty]
    private List<PurchaseDto> _purchases = new();

    [ObservableProperty]
    private PurchaseDto? _selectedPurchase;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _filterStatus = string.Empty;

    public PurchaseListViewModel()
    {
        _purchaseService = App.CurrentApp.PurchaseService;
        _ = LoadPurchasesAsync();
    }

    [RelayCommand]
    private async Task LoadPurchasesAsync()
    {
        IsLoading = true;
        Purchases = await _purchaseService.GetPurchasesAsync(FilterStatus);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task ApprovePurchase()
    {
        if (SelectedPurchase == null) return;
        var result = System.Windows.MessageBox.Show(
            $"确认审批采购单 {SelectedPurchase.PurchaseNo}？",
            "审批确认", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        await _purchaseService.ApprovePurchaseAsync(SelectedPurchase.Id, "当前用户");
        await LoadPurchasesAsync();
    }

    [RelayCommand]
    private async Task ReceivePurchase()
    {
        if (SelectedPurchase == null) return;
        var result = System.Windows.MessageBox.Show(
            $"确认入库采购单 {SelectedPurchase.PurchaseNo}？",
            "入库确认", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        await _purchaseService.ReceivePurchaseAsync(SelectedPurchase.Id);
        await LoadPurchasesAsync();
    }

    [RelayCommand]
    private async Task GenerateAssets()
    {
        if (SelectedPurchase == null) return;
        try
        {
            await _purchaseService.GenerateAssetsFromPurchaseAsync(SelectedPurchase.Id);
            System.Windows.MessageBox.Show("资产卡片已生成！", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeletePurchaseAsync(PurchaseDto? purchase)
    {
        var target = purchase ?? SelectedPurchase;
        if (target == null) return;
        var result = System.Windows.MessageBox.Show(
            $"确认删除采购单 {target.PurchaseNo}？",
            "删除确认", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        await _purchaseService.DeletePurchaseAsync(target.Id);
        await LoadPurchasesAsync();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        System.Windows.MessageBox.Show("导出功能暂未实现", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        System.Windows.MessageBox.Show("导入功能暂未实现", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    private void AddPurchase()
    {
        var nav = App.CurrentApp.NavigationService;
        nav.NavigateTo(new Views.PurchaseEditPage());
    }

    [RelayCommand]
    private void EditPurchase(PurchaseDto? purchase)
    {
        var target = purchase ?? SelectedPurchase;
        if (target == null) return;
        // TODO: 打开编辑页面
        System.Windows.MessageBox.Show("编辑功能暂未实现", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
