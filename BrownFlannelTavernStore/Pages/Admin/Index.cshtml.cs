using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages.Admin;

[Authorize(Roles = "Owner,Manager")]
public class IndexModel : PageModel
{
    private readonly StoreDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(StoreDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalUsers { get; set; }
    public List<Order> RecentOrders { get; set; } = [];

    public async Task OnGetAsync()
    {
        TotalOrders = await _context.Orders.CountAsync();
        PendingOrders = await _context.Orders.CountAsync(o =>
            o.Status == OrderStatus.Paid || o.Status == OrderStatus.Processing);
        TotalProducts = await _context.Products.CountAsync();
        TotalCategories = await _context.Products.Select(p => p.Category).Distinct().CountAsync();
        TotalUsers = _userManager.Users.Count();

        RecentOrders = await _context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .ToListAsync();
    }
}
