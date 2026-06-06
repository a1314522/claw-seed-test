using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Core.Entities;

public class Asset
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 资产编号（从金蝶同步或系统生成）
    /// </summary>
    public string AssetCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 资产名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 规格型号
    /// </summary>
    public string? Specification { get; set; }
    
    /// <summary>
    /// 序列号/SN码
    /// </summary>
    public string? SerialNumber { get; set; }
    
    /// <summary>
    /// 资产分类
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// 购置日期
    /// </summary>
    public DateTime PurchaseDate { get; set; }
    
    /// <summary>
    /// 购置价格
    /// </summary>
    public decimal PurchasePrice { get; set; }
    
    /// <summary>
    /// 供应商
    /// </summary>
    public string? Supplier { get; set; }
    
    /// <summary>
    /// 当前状态：在库/在用/维修/报废
    /// </summary>
    public AssetStatus Status { get; set; } = AssetStatus.InStock;
    
    /// <summary>
    /// 当前使用人ID
    /// </summary>
    public int? CurrentUserId { get; set; }
    public Employee? CurrentUser { get; set; }
    
    /// <summary>
    /// 当前部门ID
    /// </summary>
    public int? CurrentDepartmentId { get; set; }
    public OrganizationUnit? CurrentDepartment { get; set; }
    
    /// <summary>
    /// 存放位置
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    /// 二维码标签内容
    /// </summary>
    public string? QrCode { get; set; }
    
    /// <summary>
    /// 金蝶卡片ID（同步来源标识）
    /// </summary>
    public string? KingdeeCardId { get; set; }
    
    /// <summary>
    /// 折旧方法
    /// </summary>
    public DepreciationMethod DepreciationMethod { get; set; } = DepreciationMethod.StraightLine;
    
    /// <summary>
    /// 预计使用年限（月）
    /// </summary>
    public int UsefulLifeMonths { get; set; } = 36;
    
    /// <summary>
    /// 残值率（0-1）
    /// </summary>
    public decimal ResidualRate { get; set; } = 0.05m;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 生命周期记录
    /// </summary>
    public List<AssetLifecycle> LifecycleRecords { get; set; } = new();
}

public enum AssetStatus
{
    InStock = 0,    // 在库
    InUse = 1,      // 在用
    UnderRepair = 2, // 维修中
    Scrapped = 3,   // 已报废
    PendingApproval = 4 // 待审批
}

public enum DepreciationMethod
{
    StraightLine = 0,   // 平均年限法
    DoubleDeclining = 1 // 双倍余额递减法
}
