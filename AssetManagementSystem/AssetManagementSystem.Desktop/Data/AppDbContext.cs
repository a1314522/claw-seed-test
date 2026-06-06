using AssetManagementSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementSystem.Desktop.Data;

public class AppDbContext : DbContext
{
    public AppDbContext() { }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AssetManagementSystem",
                "data.db");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Consumable> Consumables => Set<Consumable>();
    public DbSet<ConsumableUsage> ConsumableUsages => Set<ConsumableUsage>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasIndex(e => e.AssetCode).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.KingdeeId);
        });

        modelBuilder.Entity<AssetCategory>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ParentId);
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasIndex(e => e.PurchaseNo).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Consumable>(entity =>
        {
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
        });
    }
}
