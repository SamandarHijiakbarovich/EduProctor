using EduProctor.Core;
using EduProctor.Core.Entities;
using EduProctor.Services.Interfaces;      // <<< TO'G'RI YO'L
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduProctor.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // 1. Default Organization yaratish
        if (!await context.Organizations.AnyAsync())
        {
            var defaultOrg = new Organization
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "System Organization",
                Slug = "system",
                Status = OrganizationStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            context.Organizations.Add(defaultOrg);
            await context.SaveChangesAsync();
        }

        // 2. SuperAdmin yaratish
        if (!await context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
        {
            var org = await context.Organizations.FirstOrDefaultAsync();
            if (org != null)
            {
                var superAdmin = new User
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Email = "superadmin@eduproctor.com",
                    PasswordHash = passwordHasher.Hash("Admin123456"),
                    FirstName = "Super",
                    LastName = "Admin",
                    OrganizationId = org.Id,
                    Role = UserRole.SuperAdmin,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                context.Users.Add(superAdmin);
                await context.SaveChangesAsync();
            }
        }
    }
}