using System.Windows;
using AssetManagementSystem.Desktop.Services;
using AssetManagementSystem.Desktop.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementSystem.Desktop
{
    public partial class App : Application
    {
        public static App CurrentApp => (App)Current;
        
        private AppDbContext? _dbContext;
        public AppDbContext DbContext => _dbContext ??= new AppDbContext();
        
        private NavigationService? _navigationService;
        public NavigationService NavigationService => _navigationService ??= new NavigationService();
        
        private AssetService? _assetService;
        public AssetService AssetService => _assetService ??= new AssetService(DbContext);
        
        private AssetCategoryService? _categoryService;
        public AssetCategoryService AssetCategoryService => _categoryService ??= new AssetCategoryService(DbContext);
        
        private PurchaseService? _purchaseService;
        public PurchaseService PurchaseService => _purchaseService ??= new PurchaseService(DbContext);
        
        private ConsumableService? _consumableService;
        public ConsumableService ConsumableService => _consumableService ??= new ConsumableService(DbContext);
        
        private ReportService? _reportService;
        public ReportService ReportService => _reportService ??= new ReportService(DbContext);
        
        private SystemConfigService? _configService;
        public SystemConfigService ConfigService => _configService ??= new SystemConfigService(DbContext);
        
        private KingdeeSyncService? _kingdeeSyncService;
        public KingdeeSyncService KingdeeSyncService => _kingdeeSyncService ??= new KingdeeSyncService();
        
        private AdSyncService? _adSyncService;
        public AdSyncService AdSyncService => _adSyncService ??= new AdSyncService();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var context = new AppDbContext();
            context.Database.EnsureCreated();
            SeedData.Initialize(context);
        }
    }
}
