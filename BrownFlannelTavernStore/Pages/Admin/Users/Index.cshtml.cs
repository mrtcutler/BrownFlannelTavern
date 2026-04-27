using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;

namespace BrownFlannelTavernStore.Pages.Admin.Users;

[Authorize(Roles = "Owner")]
public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public List<AdminUserViewModel> AdminUsers { get; set; } = [];
    public string? Message { get; set; }

    public async Task OnGetAsync(string? message = null)
    {
        Message = message;
        await LoadUsers();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null && user.Email != User.Identity?.Name)
        {
            await _userManager.DeleteAsync(user);
        }

        return RedirectToPage(new { message = "User deleted." });
    }

    private async Task LoadUsers()
    {
        var owners = await _userManager.GetUsersInRoleAsync(SeedData.OwnerRole);
        var managers = await _userManager.GetUsersInRoleAsync(SeedData.ManagerRole);

        AdminUsers = owners.Select(u => new AdminUserViewModel
            { Id = u.Id, Email = u.Email!, Role = SeedData.OwnerRole })
            .Concat(managers.Select(u => new AdminUserViewModel
            { Id = u.Id, Email = u.Email!, Role = SeedData.ManagerRole }))
            .ToList();
    }
}

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
