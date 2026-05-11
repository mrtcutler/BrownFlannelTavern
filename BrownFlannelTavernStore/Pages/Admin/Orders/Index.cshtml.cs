using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Utilities;

namespace BrownFlannelTavernStore.Pages.Admin.Orders;

[Authorize(Roles = "Owner,Manager")]
public class IndexModel : PageModel
{
    private readonly StoreDbContext _context;

    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    public PagedList<Order> Orders { get; set; } = new(Array.Empty<Order>(), 1, PagedListExtensions.DefaultPageSize, 0);
    public OrderStatus? StatusFilter { get; set; }
    public PaginationViewModel Pagination { get; set; } = null!;

    public async Task OnGetAsync(OrderStatus? status, int page = 1)
    {
        StatusFilter = status;

        var query = _context.Orders.AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        Orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToPagedListAsync(page);

        var routeData = new Dictionary<string, string?>();
        if (status.HasValue) routeData["status"] = status.Value.ToString();
        Pagination = PaginationViewModel.From(Orders, "/Admin/Orders/Index", routeData);
    }
}
