using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages.Admin.Products;

[Authorize(Roles = SeedData.OwnerRole)]
public class CreateModel : PageModel
{
    private readonly StoreDbContext _context;

    public CreateModel(StoreDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        _context.Products.Add(Product);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Admin/Products/Edit", new { id = Product.Id });
    }
}
