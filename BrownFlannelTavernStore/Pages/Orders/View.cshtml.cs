using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services;

namespace BrownFlannelTavernStore.Pages.Orders;

public class ViewModel : PageModel
{
    private readonly StoreDbContext _context;
    private readonly OrderViewTokenService _tokenService;

    public ViewModel(StoreDbContext context, OrderViewTokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public Order? Order { get; set; }
    public bool TokenInvalid { get; set; }

    public async Task OnGetAsync(string? token)
    {
        var orderId = _tokenService.Validate(token);
        if (orderId is null)
        {
            TokenInvalid = true;
            return;
        }

        Order = await _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (Order is null)
        {
            // Treat a missing order the same as an invalid token so we don't
            // leak information about which order IDs exist.
            TokenInvalid = true;
        }
    }
}
