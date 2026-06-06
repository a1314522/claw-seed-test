using AIKnowledgeBase.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeBase.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Knowledge Base
        public DbSet<Document> Documents { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<DocumentHistory> DocumentHistories { get; set; }
        
        // Asset Management
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetLog> AssetLogs { get; set; }
        public DbSet<ConsumableUsage> ConsumableUsages { get; set; }
        public DbSet<Department> Departments { get; set; }
        
        // System
        public DbSet<SystemUser> SystemUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Document
            modelBuilder.Entity<Document>()
                .Property(d => d.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            modelBuilder.Entity<Document>()
                .Property(d => d.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Asset
            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.AssetCode)
                .IsUnique();
                
            modelBuilder.Entity<Asset>()
                .Property(a => a.PurchasePrice)
                .HasPrecision(15, 2);
                
            modelBuilder.Entity<Asset>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // AssetLog
            modelBuilder.Entity<AssetLog>()
                .Property(l => l.ActionDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Department
            modelBuilder.Entity<Department>()
                .HasIndex(d => d.DeptCode)
                .IsUnique();
                
            modelBuilder.Entity<Department>()
                .Property(d => d.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // SystemUser
            modelBuilder.Entity<SystemUser>()
                .HasIndex(u => u.Username)
                .IsUnique();
                
            modelBuilder.Entity<SystemUser>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
