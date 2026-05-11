using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Utilities;

namespace BrownFlannelTavernStore.Pages.Admin.Users;

[Authorize(Roles = "Owner")]
public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public PagedList<AdminUserViewModel> AdminUsers { get; set; } = new(Array.Empty<AdminUserViewModel>(), 1, PagedListExtensions.DefaultPageSize, 0);
    public string? Message { get; set; }
    public PaginationViewModel Pagination { get; set; } = null!;

    public async Task OnGetAsync(string? message = null, int page = 1)
    {
        Message = message;
        await LoadUsers(page);
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

    private async Task LoadUsers(int page)
    {
        var owners = await _userManager.GetUsersInRoleAsync(SeedData.OwnerRole);
        var managers = await _userManager.GetUsersInRoleAsync(SeedData.ManagerRole);

        var allUsers = owners.Select(u => new AdminUserViewModel
            { Id = u.Id, Email = u.Email!, Role = SeedData.OwnerRole })
            .Concat(managers.Select(u => new AdminUserViewModel
            { Id = u.Id, Email = u.Email!, Role = SeedData.ManagerRole }))
            .OrderBy(u => u.Email);

        AdminUsers = allUsers.ToPagedList(page);
        Pagination = PaginationViewModel.From(AdminUsers, "/Admin/Users/Index");
    }
}

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
