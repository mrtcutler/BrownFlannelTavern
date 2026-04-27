using Microsoft.AspNetCore.Identity;

namespace BrownFlannelTavernStore.Data;

public static class SeedData
{
    public const string OwnerRole = "Owner";
    public const string ManagerRole = "Manager";

    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        // Create roles
        string[] roles = [OwnerRole, ManagerRole];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create default Owner account
        var ownerEmail = configuration["AdminSettings:OwnerEmail"] ?? "owner@brownflanneltavern.com";
        var ownerPassword = configuration["AdminSettings:OwnerPassword"] ?? "Owner123!";

        var ownerUser = await userManager.FindByEmailAsync(ownerEmail);
        if (ownerUser == null)
        {
            ownerUser = new IdentityUser
            {
                UserName = ownerEmail,
                Email = ownerEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(ownerUser, ownerPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(ownerUser, OwnerRole);
            }
        }
    }
}
