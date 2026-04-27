using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages.Orders;

public class ConfirmationModel : PageModel
{
    private readonly StoreDbContext _context;

    public ConfirmationModel(StoreDbContext context)
    {
        _context = context;
    }

    public Order? Order { get; set; }

    public async Task OnGetAsync(int id)
    {
        Order = await _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
}
