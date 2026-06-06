using System.ComponentModel.DataAnnotations;

namespace AssetManager.Core.Entities;

public class InventoryCheck
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 盘点任务名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 盘点范围：全部/指定部门/指定分类
    /// </summary>
    public string Scope { get; set; } = "All";
    
    /// <summary>
    /// 盘点状态
    /// </summary>
    public InventoryStatus Status { get; set; } = InventoryStatus.Pending;
    
    /// <summary>
    /// 计划开始时间
    /// </summary>
    public DateTime? PlannedStartTime { get; set; }
    
    /// <summary>
    /// 实际开始时间
    /// </summary>
    public DateTime? ActualStartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? CompletedTime { get; set; }
    
    /// <summary>
    /// 创建人
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 盘点明细
    /// </summary>
    public List<InventoryCheckItem> Items { get; set; } = new();
}

public enum InventoryStatus
{
    Pending = 0,     // 待开始
    InProgress = 1,  // 盘点中
    Completed = 2    // 已完成
}

public class InventoryCheckItem
{
    [Key]
    public int Id { get; set; }
    
    public int InventoryCheckId { get; set; }
    public InventoryCheck InventoryCheck { get; set; } = null!;
    
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    
    /// <summary>
    /// 账存状态
    /// </summary>
    public AssetStatus BookStatus { get; set; }
    
    /// <summary>
    /// 盘点状态：未盘/正常/盘亏/盘盈
    /// </summary>
    public CheckResult Result { get; set; } = CheckResult.NotChecked;
    
    /// <summary>
    /// 实际盘点人
    /// </summary>
    public string? CheckerName { get; set; }
    
    /// <summary>
    /// 盘点时间
    /// </summary>
    public DateTime? CheckTime { get; set; }
    
    /// <summary>
    /// 备注（异常说明）
    /// </summary>
    public string? Remarks { get; set; }
}

public enum CheckResult
{
    NotChecked = 0,  // 未盘点
    Normal = 1,      // 正常
    Shortage = 2,    // 盘亏（账有实无）
    Surplus = 3      // 盘盈（账无实有）
}
