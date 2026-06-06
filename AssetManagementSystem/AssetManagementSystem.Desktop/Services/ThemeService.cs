using System.Windows;
using AssetManagementSystem.Core.Enums;

namespace AssetManagementSystem.Desktop.Services;

public class ThemeService
{
    private ThemeMode _currentTheme = ThemeMode.浅色;

    public ThemeMode CurrentTheme => _currentTheme;

    public event Action? ThemeChanged;

    public void SetTheme(ThemeMode theme)
    {
        _currentTheme = theme;
        ApplyTheme();
        ThemeChanged?.Invoke();
    }

    private void ApplyTheme()
    {
        var dict = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme") == true);
        if (dict != null)
            Application.Current.Resources.MergedDictionaries.Remove(dict);

        var newDict = new ResourceDictionary();
        if (_currentTheme == ThemeMode.深色)
        {
            newDict["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            newDict["ForegroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
            newDict["CardBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
            newDict["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60));
            newDict["PrimaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 165, 250));
            newDict["AccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
        }
        else
        {
            newDict["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250));
            newDict["ForegroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            newDict["CardBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            newDict["BorderBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220));
            newDict["PrimaryBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235));
            newDict["AccentBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(29, 78, 216));
        }
        Application.Current.Resources.MergedDictionaries.Add(newDict);
    }
}
