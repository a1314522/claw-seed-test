# 项目目录结构

AssetManager/
├── AssetManager.sln                    # 解决方案文件
├── README.md                           # 项目说明
│
├── src/
│   ├── AssetManager.Core/              # 核心层 - 实体、枚举
│   │   ├── AssetManager.Core.csproj
│   │   └── Entities/
│   │       ├── Asset.cs                # 资产实体
│   │       ├── AssetLifecycle.cs       # 资产生命周期
│   │       ├── Consumable.cs           # 耗材实体
│   │       ├── ConsumableTransaction.cs # 耗材出入库
│   │       ├── Organization.cs         # 组织架构+人员
│   │       ├── InventoryCheck.cs       # 盘点任务+明细
│   │       ├── ApprovalFlow.cs         # 审批流
│   │       └── SystemConfig.cs         # 系统配置+同步日志
│   │
│   ├── AssetManager.Infrastructure/    # 基础设施层 - 数据访问
│   │   ├── AssetManager.Infrastructure.csproj
│   │   └── Data/
│   │       └── AssetManagerDbContext.cs # EF Core DbContext + 种子数据
│   │
│   ├── AssetManager.Api/               # API层 - REST接口
│   │   ├── AssetManager.Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Controllers/
│   │       ├── AssetsController.cs     # 资产CRUD + 领用/调拨/报废
│   │       ├── ConsumablesController.cs # 耗材CRUD + 出入库
│   │       ├── InventoryController.cs   # 盘点任务 + 扫码接口
│   │       └── SyncController.cs       # 金蝶同步接口
│   │
│   └── AssetManager.Web/               # Web层 - Blazor Server
│       ├── AssetManager.Web.csproj
│       ├── Program.cs
│       ├── App.razor
│       ├── _Imports.razor
│       ├── Pages/
│       │   ├── _Host.cshtml
│       │   ├── _Layout.cshtml
│       │   ├── Index.razor             # 首页仪表盘
│       │   ├── Error.razor
│       │   ├── Inventory/
│       │   │   └── Scan.razor          # 扫码盘点页面
│       │   └── Settings/
│       │       ├── KingdeeConfig.razor # 金蝶API配置
│       │       └── AdConfig.razor      # AD域配置
│       ├── Shared/
│       │   ├── MainLayout.razor        # 主布局
│       │   └── NavMenu.razor           # 左侧导航
│       └── wwwroot/
│           └── css/
│               └── site.css            # 样式
│
└── tests/
    └── AssetManager.Tests/             # 单元测试
        ├── AssetManager.Tests.csproj
        └── AssetTests.cs               # 资产实体测试

## 启动方式

### 1. API项目
cd src/AssetManager.Api
dotnet run
# 访问: https://localhost:5001/swagger

### 2. Web项目
cd src/AssetManager.Web
dotnet run
# 访问: https://localhost:5002

## 数据流向

```
金蝶ERP (采购/入账)
    ↓ 同步API
资产管理数据库 (SQLite)
    ↓ 本系统管理
领用 → 调拨 → 维修 → 报废 → 盘点
    ↓ 报表
管理层决策
```

## 关键技术决策

1. **数据库**: SQLite单文件 - 零配置、易备份、适合内网部署
2. **Web框架**: Blazor Server - C#全栈、实时交互、适合内网低延迟
3. **移动端适配**: Bootstrap 5 + 响应式布局，手机扫码友好
4. **金蝶集成**: 预留K3 Cloud OpenAPI接口，需补充实际调用逻辑
5. **AD域集成**: 预留System.DirectoryServices配置页面，需补充认证中间件
