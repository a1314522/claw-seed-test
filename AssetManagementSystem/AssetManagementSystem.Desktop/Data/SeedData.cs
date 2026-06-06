using AssetManagementSystem.Core.Enums;
using AssetManagementSystem.Core.Models;

namespace AssetManagementSystem.Desktop.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        if (context.AssetCategories.Any()) return;

        var categories = new List<AssetCategory>
        {
            new() { Name = "IT设备", Description = "计算机、服务器、网络设备等", SortOrder = 1 },
            new() { Name = "办公设备", Description = "打印机、复印机、投影仪等", SortOrder = 2 },
            new() { Name = "家具", Description = "办公桌、椅子、柜子等", SortOrder = 3 },
            new() { Name = "车辆", Description = "公司车辆", SortOrder = 4 },
            new() { Name = "工具", Description = "生产工具、维修工具等", SortOrder = 5 },
        };
        context.AssetCategories.AddRange(categories);
        context.SaveChanges();

        var it = categories.First(c => c.Name == "IT设备");
        var office = categories.First(c => c.Name == "办公设备");

        var subCategories = new List<AssetCategory>
        {
            new() { Name = "台式电脑", ParentId = it.Id, SortOrder = 1 },
            new() { Name = "笔记本电脑", ParentId = it.Id, SortOrder = 2 },
            new() { Name = "服务器", ParentId = it.Id, SortOrder = 3 },
            new() { Name = "网络设备", ParentId = it.Id, SortOrder = 4 },
            new() { Name = "激光打印机", ParentId = office.Id, SortOrder = 1 },
            new() { Name = "喷墨打印机", ParentId = office.Id, SortOrder = 2 },
            new() { Name = "投影仪", ParentId = office.Id, SortOrder = 3 },
        };
        context.AssetCategories.AddRange(subCategories);
        context.SaveChanges();

        var assets = new List<Asset>
        {
            new()
            {
                AssetCode = "IT-2024-001",
                Name = "联想 ThinkPad X1",
                CategoryId = subCategories[1].Id,
                Brand = "联想",
                Model = "ThinkPad X1 Carbon",
                SerialNumber = "PF123456789",
                Location = "康桥-1F-IT部",
                Department = "IT部",
                Owner = "张三",
                AdUserName = "zhangsan",
                PurchasePrice = 12999.00m,
                PurchaseDate = new DateTime(2024, 1, 15),
                Supplier = "京东企业购",
                PurchaseOrderNo = "PO-2024-001",
                Status = AssetStatus.在用,
                WarrantyPeriod = "3年",
                WarrantyExpireDate = new DateTime(2027, 1, 15),
                Notes = "高配版本，i7-1360P，32GB内存"
            },
            new()
            {
                AssetCode = "IT-2024-002",
                Name = "Dell OptiPlex 7090",
                CategoryId = subCategories[0].Id,
                Brand = "Dell",
                Model = "OptiPlex 7090 Tower",
                SerialNumber = "SN987654321",
                Location = "临港-2F-采购部",
                Department = "采购部",
                Owner = "李四",
                AdUserName = "lisi",
                PurchasePrice = 8999.00m,
                PurchaseDate = new DateTime(2024, 2, 20),
                Supplier = "戴尔直销",
                PurchaseOrderNo = "PO-2024-005",
                Status = AssetStatus.在用,
                WarrantyPeriod = "3年",
                WarrantyExpireDate = new DateTime(2027, 2, 20),
                Notes = "标准办公配置"
            },
            new()
            {
                AssetCode = "OA-2024-001",
                Name = "HP LaserJet Pro M404",
                CategoryId = subCategories[4].Id,
                Brand = "HP",
                Model = "LaserJet Pro M404dn",
                SerialNumber = "HP-PR-001",
                Location = "康桥-1F-财务部",
                Department = "财务部",
                Owner = "王五",
                AdUserName = "wangwu",
                PurchasePrice = 3599.00m,
                PurchaseDate = new DateTime(2024, 3, 1),
                Supplier = "惠普授权经销商",
                PurchaseOrderNo = "PO-2024-010",
                Status = AssetStatus.在用,
                WarrantyPeriod = "1年",
                WarrantyExpireDate = new DateTime(2025, 3, 1),
                Notes = "网络打印机，支持双面打印"
            },
            new()
            {
                AssetCode = "IT-2023-001",
                Name = "华为交换机 S5735",
                CategoryId = subCategories[3].Id,
                Brand = "华为",
                Model = "S5735-L48T4S-A1",
                SerialNumber = "HW-SW-001",
                Location = "康桥-1F-机房",
                Department = "IT部",
                Owner = "IT部",
                AdUserName = "admin",
                PurchasePrice = 15800.00m,
                PurchaseDate = new DateTime(2023, 6, 10),
                Supplier = "华为企业业务",
                PurchaseOrderNo = "PO-2023-045",
                Status = AssetStatus.在用,
                WarrantyPeriod = "3年",
                WarrantyExpireDate = new DateTime(2026, 6, 10),
                Notes = "核心交换机，48口千兆"
            },
            new()
            {
                AssetCode = "IT-2022-001",
                Name = "旧服务器 Dell R740",
                CategoryId = subCategories[2].Id,
                Brand = "Dell",
                Model = "PowerEdge R740",
                SerialNumber = "SN-OLD-001",
                Location = "临港-1F-仓库",
                Department = "IT部",
                Owner = "IT部",
                AdUserName = "admin",
                PurchasePrice = 45000.00m,
                PurchaseDate = new DateTime(2022, 1, 5),
                Supplier = "戴尔直销",
                PurchaseOrderNo = "PO-2022-001",
                Status = AssetStatus.报废,
                ScrapDate = new DateTime(2024, 12, 1),
                ScrapReason = "硬件老化，已无法满足业务需求",
                Notes = "已下线，等待回收处理"
            }
        };
        context.Assets.AddRange(assets);

        var consumables = new List<Consumable>
        {
            new()
            {
                Name = "A4打印纸",
                CategoryId = office.Id,
                Specification = "80g",
                Unit = "包",
                StockQuantity = 150,
                MinStock = 50,
                StorageLocation = "康桥-1F-仓库A区",
                Supplier = "得力文具",
                UnitPrice = 25.00m,
                Notes = "常规办公用纸"
            },
            new()
            {
                Name = "HP 88A硒鼓",
                CategoryId = subCategories[4].Id,
                Specification = "CC388A",
                Unit = "个",
                StockQuantity = 8,
                MinStock = 5,
                StorageLocation = "康桥-1F-仓库B区",
                Supplier = "惠普授权经销商",
                UnitPrice = 420.00m,
                Notes = "适用于HP LaserJet系列"
            },
            new()
            {
                Name = "网线 CAT6",
                CategoryId = subCategories[3].Id,
                Specification = "3米",
                Unit = "条",
                StockQuantity = 200,
                MinStock = 30,
                StorageLocation = "临港-1F-仓库C区",
                Supplier = "泛达网络",
                UnitPrice = 15.00m,
                Notes = "六类网线"
            }
        };
        context.Consumables.AddRange(consumables);

        context.SaveChanges();
    }
}
