using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YardOps.Data;

public static class DbSeeder
{
    public static async Task SeedRoles(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var roles = new List<ApplicationRole>
        {
            new ApplicationRole
            {
                Name = "Admin",
                Description = "System administrator with full access",
                IsSystemRole = true,
                Status = "Active"
            },
            new ApplicationRole
            {
                Name = "YardManager",
                Description = "Manages yard operations and drivers",
                IsSystemRole = true,
                Status = "Active"
            },
            new ApplicationRole
            {
                Name = "Driver",
                Description = "Driver with limited operational access",
                IsSystemRole = true,
                Status = "Active"
            }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                await roleManager.CreateAsync(role);
            }
        }
    }

    public static async Task SeedDefaultAdmin(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        string adminEmail = configuration["DefaultAdmin:Email"] ?? "admin@yardops.com";
        string adminPassword = configuration["DefaultAdmin:Password"] ?? "Admin@123";
        string firstName = configuration["DefaultAdmin:FirstName"] ?? "System";
        string lastName = configuration["DefaultAdmin:LastName"] ?? "Administrator";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = firstName,
                LastName = lastName,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}