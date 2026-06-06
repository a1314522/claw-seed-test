using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Interfaces;

namespace AssetManagementSystem.Desktop.ViewModels;

public partial class ConsumableListViewModel : ObservableObject
{
    private readonly IConsumableService _consumableService;

    [ObservableProperty]
    private List<ConsumableDto> _consumables = new();

    [ObservableProperty]
    private ConsumableDto? _selectedConsumable;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private List<ConsumableUsageDto> _usages = new();

    [ObservableProperty]
    private CreateConsumableUsageDto _newUsage = new();

    public ConsumableListViewModel()
    {
        _consumableService = App.CurrentApp.ConsumableService;
        _ = LoadConsumablesAsync();
    }

    [RelayCommand]
    private async Task LoadConsumablesAsync()
    {
        IsLoading = true;
        Consumables = await _consumableService.GetConsumablesAsync(SearchKeyword);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task RecordUsage()
    {
        if (SelectedConsumable == null) return;
        NewUsage.ConsumableId = SelectedConsumable.Id;
        try
        {
            await _consumableService.RecordUsageAsync(NewUsage);
            NewUsage = new CreateConsumableUsageDto();
            await LoadConsumablesAsync();
            await LoadUsagesAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task LoadUsagesAsync()
    {
        if (SelectedConsumable == null) return;
        Usages = await _consumableService.GetUsagesAsync(SelectedConsumable.Id);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadConsumablesAsync();
    }

    [RelayCommand]
    private void AddConsumable()
    {
        System.Windows.MessageBox.Show("新增消耗品功能暂未实现", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ViewUsage()
    {
        if (SelectedConsumable == null) return;
        _ = LoadUsagesAsync();
    }

    [RelayCommand]
    private void EditConsumable(ConsumableDto? consumable)
    {
        var target = consumable ?? SelectedConsumable;
        if (target == null) return;
        System.Windows.MessageBox.Show("编辑功能暂未实现", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task IssueConsumable(ConsumableDto? consumable)
    {
        var target = consumable ?? SelectedConsumable;
        if (target == null) return;
        System.Windows.MessageBox.Show("领用功能暂未实现", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
