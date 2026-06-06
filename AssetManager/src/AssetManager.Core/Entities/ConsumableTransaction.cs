using System.ComponentModel.DataAnnotations;

namespace AssetManager.Core.Entities;

public class ConsumableTransaction
{
    [Key]
    public int Id { get; set; }
    
    public int ConsumableId { get; set; }
    public Consumable Consumable { get; set; } = null!;
    
    /// <summary>
    /// 操作类型：入库/出库
    /// </summary>
    public TransactionType Type { get; set; }
    
    /// <summary>
    /// 数量
    /// </summary>
    public int Quantity { get; set; }
    
    /// <summary>
    /// 操作后库存
    /// </summary>
    public int StockAfter { get; set; }
    
    /// <summary>
    /// 操作人
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;
    
    /// <summary>
    /// 领用人（出库时）
    /// </summary>
    public string? ReceiverName { get; set; }
    
    /// <summary>
    /// 领用部门
    /// </summary>
    public string? Department { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remarks { get; set; }
    
    public DateTime TransactionTime { get; set; } = DateTime.Now;
}

public enum TransactionType
{
    StockIn = 0,  // 入库
    StockOut = 1  // 出库/领用
}
