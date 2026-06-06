using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AssetManagementSystem.Core.Dtos;
using AssetManagementSystem.Core.Interfaces;

namespace AssetManagementSystem.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISystemConfigService _configService;
    private readonly IKingdeeSyncService _kingdeeService;
    private readonly IAdSyncService _adService;

    [ObservableProperty]
    private SyncConfigDto _syncConfig = new();

    [ObservableProperty]
    private string _dbPath = string.Empty;

    [ObservableProperty]
    private string _kingdeeTestResult = string.Empty;

    [ObservableProperty]
    private string _adTestResult = string.Empty;

    public SettingsViewModel()
    {
        _configService = App.CurrentApp.ConfigService;
        _kingdeeService = App.CurrentApp.KingdeeSyncService;
        _adService = App.CurrentApp.AdSyncService;
        _ = LoadSettingsAsync();
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        SyncConfig = await _configService.GetSyncConfigAsync();
        DbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AssetManagement.db");
    }

    [RelayCommand]
    private async Task SaveSyncConfigAsync()
    {
        await _configService.SaveSyncConfigAsync(SyncConfig);
        System.Windows.MessageBox.Show("配置已保存", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task TestKingdeeConnectionAsync()
    {
        var ok = await _kingdeeService.TestConnectionAsync();
        KingdeeTestResult = ok ? "连接成功" : "连接失败";
    }

    [RelayCommand]
    private async Task TestAdConnectionAsync()
    {
        var ok = await _adService.TestConnectionAsync();
        AdTestResult = ok ? "连接成功" : "连接失败";
    }

    [RelayCommand]
    private void OpenDbFolder()
    {
        var folder = System.IO.Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
        {
            System.Diagnostics.Process.Start("explorer.exe", folder);
        }
    }
}
