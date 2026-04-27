using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages.Admin.Orders;

[Authorize(Roles = "Owner,Manager")]
public class IndexModel : PageModel
{
    private readonly StoreDbContext _context;

    public IndexModel(StoreDbContext context)
    {
        _context = context;
    }

    public List<Order> Orders { get; set; } = [];
    public OrderStatus? StatusFilter { get; set; }

    public async Task OnGetAsync(OrderStatus? status)
    {
        StatusFilter = status;

        var query = _context.Orders.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        Orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}
