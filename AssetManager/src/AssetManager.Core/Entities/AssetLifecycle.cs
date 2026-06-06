using System.ComponentModel.DataAnnotations;

namespace AssetManager.Core.Entities;

public class AssetLifecycle
{
    [Key]
    public int Id { get; set; }
    
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    
    /// <summary>
    /// 操作类型：入库/领用/调拨/维修/报废/归还
    /// </summary>
    public LifecycleAction Action { get; set; }
    
    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime ActionTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 操作人
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;
    
    /// <summary>
    /// 原使用人
    /// </summary>
    public string? FromUser { get; set; }
    
    /// <summary>
    /// 新使用人
    /// </summary>
    public string? ToUser { get; set; }
    
    /// <summary>
    /// 原部门
    /// </summary>
    public string? FromDepartment { get; set; }
    
    /// <summary>
    /// 新部门
    /// </summary>
    public string? ToDepartment { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
}

public enum LifecycleAction
{
    StockIn = 0,     // 入库
    Assigned = 1,    // 领用
    Transferred = 2, // 调拨
    Repaired = 3,    // 维修
    Returned = 4,    // 归还
    Scrapped = 5     // 报废
}
