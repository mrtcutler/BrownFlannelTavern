using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Models.Settings;
using BrownFlannelTavernStore.Services;
using BrownFlannelTavernStore.Services.Notifications;
using BrownFlannelTavernStore.Services.Notifications.Emails;

namespace BrownFlannelTavernStore.Pages.Admin.Orders;

[Authorize(Roles = SeedData.OwnerOrManagerRoles)]
public class DetailsModel : PageModel
{
    private readonly StoreDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<BusinessSettings> _businessOptions;
    private readonly PaymentService _paymentService;
    private readonly OrderViewTokenService _orderViewTokenService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(StoreDbContext context, IEmailSender emailSender,
        IOptions<BusinessSettings> businessOptions, PaymentService paymentService,
        OrderViewTokenService orderViewTokenService,
        ILogger<DetailsModel> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _businessOptions = businessOptions;
        _paymentService = paymentService;
        _orderViewTokenService = orderViewTokenService;
        _logger = logger;
    }

    public Order? Order { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public OrderStatus NewStatus { get; set; }

    [BindProperty]
    public string? OrderNotes { get; set; }

    public static IEnumerable<OrderStatus> ManuallyAssignableStatuses =>
        Enum.GetValues<OrderStatus>().Where(s => s != OrderStatus.Refunded);

    public bool CanRefund => Order is not null
        && Order.Status != OrderStatus.Refunded
        && Order.Status != OrderStatus.Cancelled
        && Order.Status != OrderStatus.Pending
        && !string.IsNullOrWhiteSpace(Order.StripePaymentIntentId);

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

        if (NewStatus == OrderStatus.Refunded)
        {
            ErrorMessage = "Use the Refund button to refund an order — status cannot be set to Refunded directly.";
            NewStatus = Order.Status;
            return Page();
        }

        var previousStatus = Order.Status;
        Order.Status = NewStatus;
        Order.Notes = OrderNotes;
        Order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (previousStatus != NewStatus)
        {
            try
            {
                var viewUrl = _orderViewTokenService.GetViewUrl(Order.Id);
                await _emailSender.SendAsync(OrderStatusChangeEmail.Build(Order, previousStatus, _businessOptions.Value, viewUrl));
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

    public async Task<IActionResult> OnPostRefundAsync(int id)
    {
        Order = await _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (Order == null)
            return NotFound();

        NewStatus = Order.Status;

        if (Order.Status == OrderStatus.Refunded)
        {
            ErrorMessage = "This order has already been refunded.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Order.StripePaymentIntentId))
        {
            ErrorMessage = "This order has no Stripe payment associated with it and cannot be refunded.";
            return Page();
        }

        try
        {
            var refund = await _paymentService.CreateRefundAsync(Order.StripePaymentIntentId);
            Order.Status = OrderStatus.Refunded;
            Order.RefundedAt = DateTime.UtcNow;
            Order.StripeRefundId = refund.Id;
            Order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            NewStatus = Order.Status;

            try
            {
                var viewUrl = _orderViewTokenService.GetViewUrl(Order.Id);
                await _emailSender.SendAsync(RefundConfirmationEmail.Build(Order, _businessOptions.Value, viewUrl));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send refund confirmation email for order {OrderId}", Order.Id);
            }

            Message = $"Refunded ${Order.TotalAmount:F2} to customer. Stripe refund ID: {refund.Id}.";
            return Page();
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed for order {OrderId}", Order.Id);
            ErrorMessage = $"Refund failed: {ex.Message}";
            return Page();
        }
    }
}
