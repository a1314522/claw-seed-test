using System.Windows;
using AssetManagementSystem.Desktop.ViewModels;
using AssetManagementSystem.Desktop.Services;

namespace AssetManagementSystem.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var nav = new NavigationService();
            nav.Initialize(MainContent);
            DataContext = new MainViewModel(nav);
        }
    }
}
