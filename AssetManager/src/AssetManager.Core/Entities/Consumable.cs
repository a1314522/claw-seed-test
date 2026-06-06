using System.ComponentModel.DataAnnotations;

namespace AssetManager.Core.Entities;

public class Consumable
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 物品编码
    /// </summary>
    public string ItemCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 物品名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 规格型号
    /// </summary>
    public string? Specification { get; set; }
    
    /// <summary>
    /// 分类
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前库存数量
    /// </summary>
    public int CurrentStock { get; set; }
    
    /// <summary>
    /// 预警阈值
    /// </summary>
    public int AlertThreshold { get; set; } = 10;
    
    /// <summary>
    /// 单位
    /// </summary>
    public string Unit { get; set; } = "个";
    
    /// <summary>
    /// 存放位置
    /// </summary>
    public string? StorageLocation { get; set; }
    
    /// <summary>
    /// 是否低于预警线
    /// </summary>
    public bool IsBelowAlert => CurrentStock <= AlertThreshold;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public List<ConsumableTransaction> Transactions { get; set; } = new();
}
