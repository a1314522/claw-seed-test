namespace AssetManagementSystem.Desktop.Views
{
    public partial class AssetEditPage : System.Windows.Controls.UserControl
    {
        public AssetEditPage()
        {
            InitializeComponent();
        }

        public AssetEditPage(int assetId) : this()
        {
            if (DataContext is ViewModels.AssetEditViewModel vm)
            {
                vm.Initialize(assetId);
            }
        }
    }
}
