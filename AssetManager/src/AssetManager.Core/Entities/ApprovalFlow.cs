using System.ComponentModel.DataAnnotations;

namespace AssetManager.Core.Entities;

public class ApprovalFlow
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 业务类型：领用/调拨/报废
    /// </summary>
    public string BusinessType { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务单ID
    /// </summary>
    public int BusinessId { get; set; }
    
    /// <summary>
    /// 申请人
    /// </summary>
    public string ApplicantName { get; set; } = string.Empty;
    
    /// <summary>
    /// 申请时间
    /// </summary>
    public DateTime ApplyTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 审批状态
    /// </summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    
    /// <summary>
    /// 当前审批层级
    /// </summary>
    public int CurrentLevel { get; set; } = 1;
    
    /// <summary>
    /// 审批记录
    /// </summary>
    public List<ApprovalRecord> Records { get; set; } = new();
}

public class ApprovalRecord
{
    [Key]
    public int Id { get; set; }
    
    public int ApprovalFlowId { get; set; }
    public ApprovalFlow ApprovalFlow { get; set; } = null!;
    
    /// <summary>
    /// 审批层级
    /// </summary>
    public int Level { get; set; }
    
    /// <summary>
    /// 审批人
    /// </summary>
    public string ApproverName { get; set; } = string.Empty;
    
    /// <summary>
    /// 审批结果
    /// </summary>
    public ApprovalAction Action { get; set; }
    
    /// <summary>
    /// 审批意见
    /// </summary>
    public string? Comments { get; set; }
    
    public DateTime? ApprovalTime { get; set; }
}

public enum ApprovalStatus
{
    Pending = 0,    // 待审批
    Approved = 1,   // 已通过
    Rejected = 2,   // 已驳回
    Cancelled = 3   // 已取消
}

public enum ApprovalAction
{
    Approved = 1,   // 同意
    Rejected = 2    // 驳回
}
