using System.Windows.Controls;

namespace AssetManagementSystem.Desktop.Services;

public class NavigationService
{
    private ContentControl? _contentControl;

    public void Initialize(ContentControl contentControl) => _contentControl = contentControl;

    public void NavigateTo(UserControl control)
    {
        _contentControl?.Dispatcher.Invoke(() =>
        {
            if (_contentControl != null)
                _contentControl.Content = control;
        });
    }

    public void GoBack()
    {
        // 暂不支持返回，可后续扩展
    }
}
