using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages.Products;

public class IndexModel : PageModel
{
    private readonly StoreDbContext _context;

    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    public List<Product> Products { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public string? SelectedCategory { get; set; }

    public async Task OnGetAsync(string? category)
    {
        SelectedCategory = category;

        Categories = await _context.Products
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category == category);
        }

        Products = await query.ToListAsync();
    }
}
