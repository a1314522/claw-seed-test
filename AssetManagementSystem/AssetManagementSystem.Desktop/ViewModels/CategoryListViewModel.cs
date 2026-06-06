using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Interfaces;

namespace AssetManagementSystem.Desktop.ViewModels;

public partial class CategoryListViewModel : ObservableObject
{
    private readonly IAssetCategoryService _categoryService;

    [ObservableProperty]
    private List<AssetCategoryDto> _categories = new();

    [ObservableProperty]
    private AssetCategoryDto? _selectedCategory;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    [ObservableProperty]
    private string _newCategoryDescription = string.Empty;

    [ObservableProperty]
    private int? _selectedParentId;

    public CategoryListViewModel()
    {
        _categoryService = App.CurrentApp.AssetCategoryService;
        _ = LoadCategoriesAsync();
    }

    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        IsLoading = true;
        Categories = await _categoryService.GetCategoriesAsync();
        IsLoading = false;
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName)) return;
        var dto = new CreateAssetCategoryDto
        {
            Name = NewCategoryName,
            Description = NewCategoryDescription,
            ParentId = SelectedParentId
        };
        await _categoryService.CreateCategoryAsync(dto);
        NewCategoryName = string.Empty;
        NewCategoryDescription = string.Empty;
        SelectedParentId = null;
        await LoadCategoriesAsync();
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory == null) return;
        var result = System.Windows.MessageBox.Show(
            $"确认删除分类 {SelectedCategory.Name}？",
            "删除确认", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            await _categoryService.DeleteCategoryAsync(SelectedCategory.Id);
            await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
