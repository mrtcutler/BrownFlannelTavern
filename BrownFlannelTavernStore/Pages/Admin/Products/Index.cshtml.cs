using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public List<string> AvailableCategories { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? CategoryFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? NameSearchFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortDir { get; set; }

    public async Task OnGetAsync(int page = 1)
    {
        AvailableCategories = await _context.Products
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var query = _context.Products.Include(p => p.Variants).AsQueryable();

        if (!string.IsNullOrWhiteSpace(CategoryFilter))
            query = query.Where(p => p.Category == CategoryFilter);
        if (!string.IsNullOrWhiteSpace(NameSearchFilter))
        {
            var search = NameSearchFilter.Trim();
            query = query.Where(p => p.Name.Contains(search));
        }

        query = (SortBy?.ToLowerInvariant(), SortDir?.ToLowerInvariant()) switch
        {
            ("id", "asc") => query.OrderBy(p => p.Id),
            ("id", _) => query.OrderByDescending(p => p.Id),
            ("category", "asc") => query.OrderBy(p => p.Category),
            ("category", _) => query.OrderByDescending(p => p.Category),
            ("price", "asc") => query.OrderBy(p => p.Price),
            ("price", _) => query.OrderByDescending(p => p.Price),
            ("name", "desc") => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };

        Products = await query.ToPagedListAsync(page);
        Pagination = PaginationViewModel.From(Products, "/Admin/Products/Index", BuildRouteData());
    }

    public Dictionary<string, string?> BuildRouteData()
    {
        var data = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(CategoryFilter)) data[nameof(CategoryFilter)] = CategoryFilter;
        if (!string.IsNullOrWhiteSpace(NameSearchFilter)) data[nameof(NameSearchFilter)] = NameSearchFilter;
        if (!string.IsNullOrWhiteSpace(SortBy)) data[nameof(SortBy)] = SortBy;
        if (!string.IsNullOrWhiteSpace(SortDir)) data[nameof(SortDir)] = SortDir;
        return data;
    }
}
