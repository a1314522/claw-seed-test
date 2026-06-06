using AssetManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Infrastructure.Data;

public class AssetManagerDbContext : DbContext
{
    public AssetManagerDbContext(DbContextOptions<AssetManagerDbContext> options) : base(options) { }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetLifecycle> AssetLifecycles => Set<AssetLifecycle>();
    public DbSet<Consumable> Consumables => Set<Consumable>();
    public DbSet<ConsumableTransaction> ConsumableTransactions => Set<ConsumableTransaction>();
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<InventoryCheck> InventoryChecks => Set<InventoryCheck>();
    public DbSet<InventoryCheckItem> InventoryCheckItems => Set<InventoryCheckItem>();
    public DbSet<ApprovalFlow> ApprovalFlows => Set<ApprovalFlow>();
    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();
    public DbSet<KingdeeSyncLog> KingdeeSyncLogs => Set<KingdeeSyncLog>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 资产编号唯一索引
        modelBuilder.Entity<Asset>()
            .HasIndex(a => a.AssetCode)
            .IsUnique();

        // 金蝶卡片ID唯一索引（如果有值）
        modelBuilder.Entity<Asset>()
            .HasIndex(a => a.KingdeeCardId)
            .IsUnique()
            .HasFilter("[KingdeeCardId] IS NOT NULL");

        // 部门编码唯一索引
        modelBuilder.Entity<OrganizationUnit>()
            .HasIndex(o => o.Code)
            .IsUnique();

        // 人员工号唯一索引
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.EmployeeNo)
            .IsUnique();

        // 系统配置Key唯一索引
        modelBuilder.Entity<SystemConfig>()
            .HasIndex(s => s.ConfigKey)
            .IsUnique();

        // 资产配置默认数据
        modelBuilder.Entity<SystemConfig>().HasData(
            new SystemConfig { Id = 1, ConfigKey = "Kingdee.ApiUrl", ConfigValue = "", Description = "金蝶API地址", Group = "Kingdee" },
            new SystemConfig { Id = 2, ConfigKey = "Kingdee.AppId", ConfigValue = "", Description = "金蝶应用ID", Group = "Kingdee" },
            new SystemConfig { Id = 3, ConfigKey = "Kingdee.AppSecret", ConfigValue = "", Description = "金蝶应用密钥", Group = "Kingdee" },
            new SystemConfig { Id = 4, ConfigKey = "Kingdee.LastSyncTime", ConfigValue = "", Description = "上次同步时间", Group = "Kingdee" },
            new SystemConfig { Id = 5, ConfigKey = "AD.Server", ConfigValue = "", Description = "AD域服务器地址", Group = "AD" },
            new SystemConfig { Id = 6, ConfigKey = "AD.Port", ConfigValue = "389", Description = "AD域服务器端口", Group = "AD" },
            new SystemConfig { Id = 7, ConfigKey = "AD.BaseDN", ConfigValue = "", Description = "AD域BaseDN", Group = "AD" },
            new SystemConfig { Id = 8, ConfigKey = "AD.Domain", ConfigValue = "", Description = "AD域名", Group = "AD" },
            new SystemConfig { Id = 9, ConfigKey = "AD.Enabled", ConfigValue = "false", Description = "是否启用AD认证", Group = "AD" },
            new SystemConfig { Id = 10, ConfigKey = "Company.Name", ConfigValue = "乐孜芯创半导体设备（上海）有限公司", Description = "公司名称", Group = "General" }
        );
    }
}
