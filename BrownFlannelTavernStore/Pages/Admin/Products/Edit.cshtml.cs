using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages.Admin.Products;

[Authorize(Roles = SeedData.OwnerRole)]
public class EditModel : PageModel
{
    private readonly StoreDbContext _context;

    public EditModel(StoreDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Product Product { get; set; } = null!;

    [BindProperty]
    public ProductVariant NewVariant { get; set; } = new();

    public string? Message { get; set; }

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
        // Reload variants since they aren't bound in the main form
        var existing = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == Product.Id);

        if (existing == null)
            return NotFound();

        existing.Name = Product.Name;
        existing.Description = Product.Description;
        existing.Price = Product.Price;
        existing.Category = Product.Category;
        existing.ImageUrl = Product.ImageUrl;

        await _context.SaveChangesAsync();

        Product = existing;
        Message = "Product updated successfully.";
        return Page();
    }

    public async Task<IActionResult> OnPostAddVariantAsync(int productId)
    {
        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return NotFound();

        var variant = new ProductVariant
        {
            ProductId = productId,
            Size = NewVariant.Size,
            Color = NewVariant.Color,
            StockQuantity = NewVariant.StockQuantity
        };

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        return RedirectToPage(new { id = productId });
    }

    public async Task<IActionResult> OnPostDeleteVariantAsync(int variantId)
    {
        var variant = await _context.ProductVariants.FindAsync(variantId);

        if (variant == null)
            return NotFound();

        var productId = variant.ProductId;
        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();

        return RedirectToPage(new { id = productId });
    }
}
