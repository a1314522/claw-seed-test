using AssetManager.Core.Entities;
using AssetManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Tests;

public class AssetTests : IDisposable
{
    private readonly AssetManagerDbContext _context;

    public AssetTests()
    {
        var options = new DbContextOptionsBuilder<AssetManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AssetManagerDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void CanCreateAsset()
    {
        var asset = new Asset
        {
            AssetCode = "PC-202506-0001",
            Name = "联想ThinkPad T14",
            Category = "电脑",
            PurchaseDate = DateTime.Now,
            PurchasePrice = 8000m,
            Status = AssetStatus.InStock
        };

        _context.Assets.Add(asset);
        _context.SaveChanges();

        Assert.True(asset.Id > 0);
        Assert.Equal("PC-202506-0001", asset.AssetCode);
    }

    [Fact]
    public void AssetCodeMustBeUnique()
    {
        var asset1 = new Asset { AssetCode = "TEST-001", Name = "Test1", Category = "Test", PurchaseDate = DateTime.Now, PurchasePrice = 1m };
        var asset2 = new Asset { AssetCode = "TEST-001", Name = "Test2", Category = "Test", PurchaseDate = DateTime.Now, PurchasePrice = 1m };

        _context.Assets.Add(asset1);
        _context.SaveChanges();

        _context.Assets.Add(asset2);
        Assert.Throws<DbUpdateException>(() => _context.SaveChanges());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
