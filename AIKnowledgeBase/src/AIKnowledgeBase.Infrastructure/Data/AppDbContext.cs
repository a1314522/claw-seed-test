using Microsoft.EntityFrameworkCore;
using AIKnowledgeBase.Core.Entities;

namespace AIKnowledgeBase.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<SearchHistory> SearchHistories => Set<SearchHistory>();
    public DbSet<UserCategoryAccess> UserCategoryAccesses => Set<UserCategoryAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique();

        modelBuilder.Entity<UserRole>()
            .HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique();

        modelBuilder.Entity<UserCategoryAccess>()
            .HasIndex(uca => new { uca.UserId, uca.CategoryId })
            .IsUnique();

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "默认分类", Description = "未分类文档", IsPublic = true }
        );

        modelBuilder.Entity<Permission>().HasData(
            Enum.GetValues(typeof(Core.Enums.PermissionType))
                .Cast<Core.Enums.PermissionType>()
                .Select((p, i) => new Permission
                {
                    Id = i + 1,
                    Name = p.ToString(),
                    Type = p,
                    Description = GetPermissionDescription(p)
                })
        );

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "超级管理员", Description = "系统最高权限" },
            new Role { Id = 2, Name = "编辑者", Description = "可上传和管理文档" },
            new Role { Id = 3, Name = "普通用户", Description = "仅可查看和搜索" }
        );

        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { Id = 1, RoleId = 1, PermissionId = 1 },
            new RolePermission { Id = 2, RoleId = 1, PermissionId = 2 },
            new RolePermission { Id = 3, RoleId = 2, PermissionId = 9 },
            new RolePermission { Id = 4, RoleId = 2, PermissionId = 10 },
            new RolePermission { Id = 5, RoleId = 3, PermissionId = 9 }
        );
    }

    private static string GetPermissionDescription(Core.Enums.PermissionType type) => type switch
    {
        Core.Enums.PermissionType.UserView => "查看用户列表",
        Core.Enums.PermissionType.UserCreate => "创建用户",
        Core.Enums.PermissionType.UserEdit => "编辑用户",
        Core.Enums.PermissionType.UserDelete => "删除用户",
        Core.Enums.PermissionType.RoleManage => "管理角色和权限",
        Core.Enums.PermissionType.CategoryView => "查看分类",
        Core.Enums.PermissionType.CategoryCreate => "创建分类",
        Core.Enums.PermissionType.CategoryEdit => "编辑分类",
        Core.Enums.PermissionType.CategoryDelete => "删除分类",
        Core.Enums.PermissionType.DocumentView => "查看文档",
        Core.Enums.PermissionType.DocumentUpload => "上传文档",
        Core.Enums.PermissionType.DocumentDelete => "删除文档",
        Core.Enums.PermissionType.DocumentManage => "管理所有文档",
        Core.Enums.PermissionType.SearchAll => "搜索所有分类",
        Core.Enums.PermissionType.HistoryView => "查看搜索历史",
        Core.Enums.PermissionType.HistoryClear => "清空搜索历史",
        Core.Enums.PermissionType.SystemManage => "系统管理",
        _ => "未知权限"
    };
}
