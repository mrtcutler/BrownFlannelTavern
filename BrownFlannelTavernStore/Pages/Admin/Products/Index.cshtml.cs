using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages.Admin.Products;

[Authorize(Roles = "Owner")]
public class IndexModel : PageModel
{
    private readonly StoreDbContext _context;

    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    public List<Product> Products { get; set; } = [];

    public async Task OnGetAsync()
    {
        Products = await _context.Products
            .Include(p => p.Variants)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
