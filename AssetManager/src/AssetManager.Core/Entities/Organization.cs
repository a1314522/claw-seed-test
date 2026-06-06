using System.ComponentModel.DataAnnotations;

namespace AssetManager.Core.Entities;

public class OrganizationUnit
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 金蝶部门ID
    /// </summary>
    public string? KingdeeId { get; set; }
    
    /// <summary>
    /// 部门编码
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// 部门名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 父部门ID
    /// </summary>
    public int? ParentId { get; set; }
    public OrganizationUnit? Parent { get; set; }
    
    /// <summary>
    /// 子部门
    /// </summary>
    public List<OrganizationUnit> Children { get; set; } = new();
    
    /// <summary>
    /// 部门员工
    /// </summary>
    public List<Employee> Employees { get; set; } = new();
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Employee
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 金蝶人员ID
    /// </summary>
    public string? KingdeeId { get; set; }
    
    /// <summary>
    /// 工号
    /// </summary>
    public string EmployeeNo { get; set; } = string.Empty;
    
    /// <summary>
    /// 姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 部门ID
    /// </summary>
    public int DepartmentId { get; set; }
    public OrganizationUnit Department { get; set; } = null!;
    
    /// <summary>
    /// 职位
    /// </summary>
    public string? Position { get; set; }
    
    /// <summary>
    /// 手机号
    /// </summary>
    public string? Phone { get; set; }
    
    /// <summary>
    /// AD域账号
    /// </summary>
    public string? AdAccount { get; set; }
    
    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
