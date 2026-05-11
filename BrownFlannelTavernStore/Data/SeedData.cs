using Microsoft.AspNetCore.Identity;

namespace BrownFlannelTavernStore.Data;

public static class SeedData
{
    public const string OwnerRole = "Owner";
    public const string ManagerRole = "Manager";
    public const string OwnerOrManagerRoles = $"{OwnerRole},{ManagerRole}";

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
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join("; ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new InvalidOperationException($"Failed to create role '{role}': {errors}");
                }
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

            var createResult = await userManager.CreateAsync(ownerUser, ownerPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Failed to create owner user '{ownerEmail}': {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(ownerUser, OwnerRole))
        {
            var roleAssignResult = await userManager.AddToRoleAsync(ownerUser, OwnerRole);
            if (!roleAssignResult.Succeeded)
            {
                var errors = string.Join("; ", roleAssignResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Failed to assign '{OwnerRole}' role to '{ownerEmail}': {errors}");
            }
        }
    }
}
