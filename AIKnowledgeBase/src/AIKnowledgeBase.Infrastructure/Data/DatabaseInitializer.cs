using Microsoft.EntityFrameworkCore;
using AIKnowledgeBase.Infrastructure.Identity;

namespace AIKnowledgeBase.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        if (!context.Users.Any())
        {
            var hasher = new PasswordHasher();
            var admin = new Core.Entities.User
            {
                Username = "admin",
                PasswordHash = hasher.HashPassword("admin123"),
                IsAdmin = true,
                IsActive = true
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new Core.Entities.UserRole
            {
                UserId = admin.Id,
                RoleId = 1
            });
            await context.SaveChangesAsync();
        }
    }
}
