using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Models.Settings;
using BrownFlannelTavernStore.Services;
using BrownFlannelTavernStore.Services.Notifications;
using BrownFlannelTavernStore.Services.Notifications.Emails;

namespace BrownFlannelTavernStore.Pages.Checkout;

public class IndexModel : PageModel
{
    private readonly CartService _cartService;
    private readonly PaymentService _paymentService;
    private readonly StoreDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<BusinessSettings> _businessOptions;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(CartService cartService, PaymentService paymentService,
        StoreDbContext context, IConfiguration configuration,
        IEmailSender emailSender, IOptions<BusinessSettings> businessOptions,
        ILogger<IndexModel> logger)
    {
        _cartService = cartService;
        _paymentService = paymentService;
        _context = context;
        _configuration = configuration;
        _emailSender = emailSender;
        _businessOptions = businessOptions;
        _logger = logger;
    }

    public List<CartItem> CartItems { get; set; } = [];
    public decimal Total { get; set; }
    public string ClientSecret { get; set; } = string.Empty;
    public string StripePublishableKey => _configuration["Stripe:PublishableKey"] ?? string.Empty;

    [BindProperty]
    public string PaymentIntentId { get; set; } = string.Empty;

    [BindProperty]
    public string CustomerName { get; set; } = string.Empty;

    [BindProperty]
    public string CustomerEmail { get; set; } = string.Empty;

    [BindProperty]
    public string? Phone { get; set; }

    [BindProperty]
    public FulfillmentMethod FulfillmentMethod { get; set; } = FulfillmentMethod.Shipped;

    [BindProperty]
    public string? ShippingAddress { get; set; }

    [BindProperty]
    public string? City { get; set; }

    [BindProperty]
    public string? State { get; set; }

    [BindProperty]
    public string? ZipCode { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CartItems = _cartService.GetCart();
        Total = _cartService.GetTotal();

        if (!CartItems.Any())
            return Page();

        var paymentIntent = await _paymentService.CreatePaymentIntentAsync(Total);
        ClientSecret = paymentIntent.ClientSecret;
        PaymentIntentId = paymentIntent.Id;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CartItems = _cartService.GetCart();
        Total = _cartService.GetTotal();

        if (!CartItems.Any())
            return RedirectToPage("/Cart/Index");

        if (FulfillmentMethod == FulfillmentMethod.Shipped)
        {
            if (string.IsNullOrWhiteSpace(ShippingAddress))
                ModelState.AddModelError(nameof(ShippingAddress), "Shipping address is required.");
            if (string.IsNullOrWhiteSpace(City))
                ModelState.AddModelError(nameof(City), "City is required.");
            if (string.IsNullOrWhiteSpace(State))
                ModelState.AddModelError(nameof(State), "State is required.");
            if (string.IsNullOrWhiteSpace(ZipCode))
                ModelState.AddModelError(nameof(ZipCode), "Zip code is required.");
        }

        if (!ModelState.IsValid)
        {
            var freshIntent = await _paymentService.CreatePaymentIntentAsync(Total);
            ClientSecret = freshIntent.ClientSecret;
            PaymentIntentId = freshIntent.Id;
            return Page();
        }

        var paymentIntent = await _paymentService.GetPaymentIntentAsync(PaymentIntentId);

        if (paymentIntent.Status != "succeeded")
        {
            return RedirectToPage("/Checkout/Index");
        }

        var expectedAmountInCents = (long)(Total * 100);
        if (paymentIntent.Amount != expectedAmountInCents)
        {
            ModelState.AddModelError(string.Empty,
                "Cart total has changed since payment was authorized. Please review your cart and try again.");
            var freshIntent = await _paymentService.CreatePaymentIntentAsync(Total);
            ClientSecret = freshIntent.ClientSecret;
            PaymentIntentId = freshIntent.Id;
            return Page();
        }

        var order = new Order
        {
            StripePaymentIntentId = PaymentIntentId,
            CustomerEmail = CustomerEmail,
            CustomerName = CustomerName,
            Phone = Phone,
            FulfillmentMethod = FulfillmentMethod,
            NotificationPreference = NotificationPreference.Email,
            ShippingAddress = FulfillmentMethod == FulfillmentMethod.Shipped ? ShippingAddress : null,
            City = FulfillmentMethod == FulfillmentMethod.Shipped ? City : null,
            State = FulfillmentMethod == FulfillmentMethod.Shipped ? State : null,
            ZipCode = FulfillmentMethod == FulfillmentMethod.Shipped ? ZipCode : null,
            TotalAmount = Total,
            Status = OrderStatus.Paid,
            Lines = CartItems.Select(item => new OrderLine
            {
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                ProductName = item.ProductName,
                Size = item.Size,
                Color = item.Color,
                UnitPrice = item.Price,
                Quantity = item.Quantity
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var business = _businessOptions.Value;

        try
        {
            await _emailSender.SendAsync(OrderConfirmationEmail.Build(order, business));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation email for order {OrderId}", order.Id);
        }

        try
        {
            var adminEmail = _configuration["AdminSettings:OwnerEmail"];
            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                await _emailSender.SendAsync(AdminNewOrderEmail.Build(order, adminEmail, business));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin new-order alert for order {OrderId}", order.Id);
        }

        _cartService.ClearCart();

        return RedirectToPage("/Orders/Confirmation", new { id = order.Id });
    }
}
