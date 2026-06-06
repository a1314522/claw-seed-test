using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIKnowledgeBase.Core.Entities
{
    public class Asset
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string AssetCode { get; set; } = "";
        
        [Required]
        [StringLength(255)]
        public string AssetName { get; set; } = "";
        
        [StringLength(50)]
        public string? AssetType { get; set; } // fixed_asset, consumable, low_value
        
        [StringLength(100)]
        public string? Category { get; set; }
        
        public Guid? DepartmentId { get; set; }
        
        public Guid? UserId { get; set; }
        
        public DateTime? PurchaseDate { get; set; }
        
        [Column(TypeName = "decimal(15,2)")]
        public decimal? PurchasePrice { get; set; }
        
        [StringLength(255)]
        public string? Vendor { get; set; }
        
        public int? WarrantyPeriod { get; set; } // months
        
        [StringLength(20)]
        public string Status { get; set; } = "in_use"; // in_use, maintenance, scrap, transfer
        
        [StringLength(255)]
        public string? Location { get; set; }
        
        public string? Specs { get; set; } // JSON
        
        public DateTime? ScrapDate { get; set; }
        
        [StringLength(500)]
        public string? ScrapReason { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
    
    public class AssetLog
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid AssetId { get; set; }
        
        [StringLength(50)]
        public string ActionType { get; set; } = "";
        
        public Guid? FromUserId { get; set; }
        
        public Guid? ToUserId { get; set; }
        
        public Guid? FromDepartmentId { get; set; }
        
        public Guid? ToDepartmentId { get; set; }
        
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
        
        public string? Remark { get; set; }
        
        [StringLength(100)]
        public string? OperatedBy { get; set; }
    }
    
    public class ConsumableUsage
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid ConsumableId { get; set; }
        
        public Guid? UserId { get; set; }
        
        public Guid? DepartmentId { get; set; }
        
        public int Quantity { get; set; }
        
        public DateTime UsageDate { get; set; } = DateTime.UtcNow;
        
        public string? Purpose { get; set; }
        
        public Guid? ApprovedBy { get; set; }
    }
    
    public class Department
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string DeptCode { get; set; } = "";
        
        [Required]
        [StringLength(100)]
        public string DeptName { get; set; } = "";
        
        public Guid? ParentId { get; set; }
        
        public Guid? ManagerId { get; set; }
        
        public int Level { get; set; } = 1;
        
        [StringLength(20)]
        public string Source { get; set; } = "manual"; // manual, kingdee, ad
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public class SystemUser
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = "";
        
        [StringLength(255)]
        public string? PasswordHash { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(100)]
        public string? DisplayName { get; set; }
        
        public Guid? DepartmentId { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsAdmin { get; set; } = false;
        
        [StringLength(500)]
        public string? LdapDn { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
    }
}
