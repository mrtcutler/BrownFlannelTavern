using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages.Admin.Products;

[Authorize(Roles = SeedData.OwnerRole)]
public class DeleteModel : PageModel
{
    private readonly StoreDbContext _context;

    public DeleteModel(StoreDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Product Product { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        Product = product;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == Product.Id);

        if (product == null)
            return NotFound();

        _context.ProductVariants.RemoveRange(product.Variants);
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Admin/Products/Index");
    }
}
