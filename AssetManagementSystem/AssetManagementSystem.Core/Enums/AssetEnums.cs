namespace AssetManagementSystem.Core.Enums;

public enum AssetStatus
{
    采购中 = 0,
    入库 = 1,
    在用 = 2,
    维修 = 3,
    报废 = 4
}

public enum PurchaseStatus
{
    待申请 = 0,
    审批中 = 1,
    已审批 = 2,
    已采购 = 3,
    已入库 = 4,
    已取消 = 5
}

public enum SyncStatus
{
    未同步 = 0,
    同步中 = 1,
    已同步 = 2,
    同步失败 = 3
}

public enum ThemeMode
{
    浅色 = 0,
    深色 = 1,
    跟随系统 = 2
}
