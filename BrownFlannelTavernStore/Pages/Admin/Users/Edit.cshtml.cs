using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;

namespace BrownFlannelTavernStore.Pages.Admin.Users;

[Authorize(Roles = SeedData.OwnerRole)]
public class EditModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;

    public EditModel(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public string UserId { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Role { get; set; } = string.Empty;

    [BindProperty]
    public string? NewPassword { get; set; }

    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        UserId = user.Id;
        Email = user.Email!;

        var roles = await _userManager.GetRolesAsync(user);
        Role = roles.FirstOrDefault() ?? SeedData.ManagerRole;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null)
            return NotFound();

        Email = user.Email!;

        // Update role
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, Role);
        }

        // Update password if provided
        if (!string.IsNullOrEmpty(NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, NewPassword);

            if (!result.Succeeded)
            {
                Errors = result.Errors.Select(e => e.Description).ToList();
                return Page();
            }
        }

        Message = "User updated successfully.";
        return Page();
    }
}
