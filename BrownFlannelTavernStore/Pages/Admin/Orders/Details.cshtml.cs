using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services.Notifications;
using BrownFlannelTavernStore.Services.Notifications.Emails;

namespace BrownFlannelTavernStore.Pages.Admin.Orders;

[Authorize(Roles = "Owner,Manager")]
public class DetailsModel : PageModel
{
    private readonly StoreDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(StoreDbContext context, IEmailSender emailSender, ILogger<DetailsModel> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
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

        var previousStatus = Order.Status;
        Order.Status = NewStatus;
        Order.Notes = OrderNotes;
        Order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (previousStatus != NewStatus)
        {
            try
            {
                await _emailSender.SendAsync(OrderStatusChangeEmail.Build(Order, previousStatus));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status-change email for order {OrderId}", Order.Id);
            }
        }

        Message = previousStatus != NewStatus
            ? $"Order status updated to {NewStatus}."
            : "Order saved.";
        return Page();
    }
}
