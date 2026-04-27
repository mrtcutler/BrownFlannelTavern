using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages;

public class IndexModel : PageModel
{
    private readonly StoreDbContext _context;

    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    public List<Product> FeaturedProducts { get; set; } = [];

    public async Task OnGetAsync()
    {
        FeaturedProducts = await _context.Products
            .Take(3)
            .ToListAsync();
    }
}
