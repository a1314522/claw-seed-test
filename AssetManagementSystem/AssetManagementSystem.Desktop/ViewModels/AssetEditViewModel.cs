using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Desktop.Services;

namespace AssetManagementSystem.Desktop.ViewModels;

public partial class AssetEditViewModel : ObservableObject
{
    private readonly AssetService _assetService;
    private readonly AssetCategoryService _categoryService;

    [ObservableProperty]
    private CreateAssetDto _asset = new();

    [ObservableProperty]
    private List<AssetCategoryDto> _categories = new();

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _pageTitle = "新增资产";

    [ObservableProperty]
    private List<string> _statusOptions = new() { "采购中", "入库", "在用", "维修", "报废" };

    private int? _editId;

    public AssetEditViewModel()
    {
        _assetService = App.CurrentApp.AssetService;
        _categoryService = App.CurrentApp.AssetCategoryService;
        _ = LoadCategoriesAsync();
    }

    public void Initialize(int? id = null)
    {
        _editId = id;
        if (id.HasValue)
        {
            IsEditing = true;
            PageTitle = "编辑资产";
            _ = LoadAssetAsync(id.Value);
        }
    }

    private async Task LoadAssetAsync(int id)
    {
        var dto = await _assetService.GetAssetByIdAsync(id);
        if (dto != null)
        {
            Asset = new UpdateAssetDto
            {
                Id = dto.Id,
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
                Status = dto.Status
            };
        }
    }

    private async Task LoadCategoriesAsync()
    {
        Categories = await _categoryService.GetCategoriesAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (IsEditing && _editId.HasValue)
            {
                await _assetService.UpdateAssetAsync(_editId.Value, (UpdateAssetDto)Asset);
            }
            else
            {
                await _assetService.CreateAssetAsync(Asset);
            }
            GoBack();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"保存失败：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        var nav = App.CurrentApp.NavigationService;
        nav.NavigateTo(new Views.AssetListPage());
    }
}
