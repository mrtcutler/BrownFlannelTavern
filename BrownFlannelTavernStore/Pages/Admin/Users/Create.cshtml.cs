using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;

namespace BrownFlannelTavernStore.Pages.Admin.Users;

[Authorize(Roles = SeedData.OwnerRole)]
public class CreateModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;

    public CreateModel(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string Role { get; set; } = SeedData.ManagerRole;

    public List<string> Errors { get; set; } = [];

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = new IdentityUser
        {
            UserName = Email,
            Email = Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Password);

        if (!result.Succeeded)
        {
            Errors = result.Errors.Select(e => e.Description).ToList();
            return Page();
        }

        await _userManager.AddToRoleAsync(user, Role);

        return RedirectToPage("/Admin/Users/Index", new { message = $"User {Email} created as {Role}." });
    }
}
