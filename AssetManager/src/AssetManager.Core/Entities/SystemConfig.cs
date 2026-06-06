using System.ComponentModel.DataAnnotations;

namespace AssetManager.Core.Entities;

public class KingdeeSyncLog
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 同步类型：Department/Employee/AssetCard
    /// </summary>
    public string SyncType { get; set; } = string.Empty;
    
    /// <summary>
    /// 同步时间
    /// </summary>
    public DateTime SyncTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 同步结果：Success/Failed/Partial
    /// </summary>
    public string Result { get; set; } = string.Empty;
    
    /// <summary>
    /// 同步数量
    /// </summary>
    public int RecordCount { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 请求参数（JSON）
    /// </summary>
    public string? RequestPayload { get; set; }
    
    /// <summary>
    /// 响应内容（JSON）
    /// </summary>
    public string? ResponsePayload { get; set; }
}

public class SystemConfig
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 配置项Key
    /// </summary>
    public string ConfigKey { get; set; } = string.Empty;
    
    /// <summary>
    /// 配置值
    /// </summary>
    public string ConfigValue { get; set; } = string.Empty;
    
    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 分组：Kingdee/AD/General
    /// </summary>
    public string Group { get; set; } = "General";
    
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
