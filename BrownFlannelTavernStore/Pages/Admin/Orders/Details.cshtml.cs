using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Pages.Admin.Orders;

[Authorize(Roles = "Owner,Manager")]
public class DetailsModel : PageModel
{
    private readonly StoreDbContext _context;

    public DetailsModel(StoreDbContext context)
    {
        _context = context;
    }

    public Order? Order { get; set; }
    public string? Message { get; set; }

    [BindProperty]
    public OrderStatus NewStatus { get; set; }

    [BindProperty]
    public string? OrderNotes { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Order = await _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (Order == null)
            return NotFound();

        NewStatus = Order.Status;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Order = await _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (Order == null)
            return NotFound();

        Order.Status = NewStatus;
        Order.Notes = OrderNotes;
        Order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        Message = $"Order updated to {NewStatus}.";
        return Page();
    }
}
