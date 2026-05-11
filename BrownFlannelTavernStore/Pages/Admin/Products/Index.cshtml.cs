using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Utilities;

namespace BrownFlannelTavernStore.Pages.Admin.Products;

[Authorize(Roles = "Owner")]
public class IndexModel : PageModel
{
    private readonly StoreDbContext _context;

    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    public PagedList<Product> Products { get; set; } = new(Array.Empty<Product>(), 1, PagedListExtensions.DefaultPageSize, 0);
    public PaginationViewModel Pagination { get; set; } = null!;

    public async Task OnGetAsync(int page = 1)
    {
        Products = await _context.Products
            .Include(p => p.Variants)
            .OrderBy(p => p.Name)
            .ToPagedListAsync(page);

        Pagination = PaginationViewModel.From(Products, "/Admin/Products/Index");
    }
}
