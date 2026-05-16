using System.Text.Json.Serialization;
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
    private readonly OrderViewTokenService _orderViewTokenService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(CartService cartService, PaymentService paymentService,
        StoreDbContext context, IConfiguration configuration,
        IEmailSender emailSender, IOptions<BusinessSettings> businessOptions,
        OrderViewTokenService orderViewTokenService,
        ILogger<IndexModel> logger)
    {
        _cartService = cartService;
        _paymentService = paymentService;
        _context = context;
        _configuration = configuration;
        _emailSender = emailSender;
        _businessOptions = businessOptions;
        _orderViewTokenService = orderViewTokenService;
        _logger = logger;
    }

    public List<CartItem> CartItems { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
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
        Subtotal = _cartService.GetTotal();
        Total = Subtotal;

        if (!CartItems.Any())
            return Page();

        var paymentIntent = await _paymentService.CreatePaymentIntentAsync(Subtotal);
        ClientSecret = paymentIntent.ClientSecret;
        PaymentIntentId = paymentIntent.Id;

        return Page();
    }

    public async Task<IActionResult> OnPostCalculateTaxAsync([FromBody] TaxRecalcRequest? request)
    {
        if (request is null)
            return BadRequest(new { error = "Invalid request body." });

        CartItems = _cartService.GetCart();
        Subtotal = _cartService.GetTotal();

        if (!CartItems.Any() || Subtotal <= 0)
            return BadRequest(new { error = "Cart is empty." });

        if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
            return BadRequest(new { error = "Missing payment intent." });

        var business = _businessOptions.Value;
        TaxAddress? address = ResolveTaxAddress(request, business);

        if (address is null)
        {
            await _paymentService.UpdatePaymentIntentAmountAsync(request.PaymentIntentId, Subtotal);
            return new JsonResult(new
            {
                subtotal = Subtotal,
                tax = 0m,
                total = Subtotal,
                calculationId = (string?)null,
            });
        }

        try
        {
            var calc = await _paymentService.CalculateTaxAsync(Subtotal, address);
            await _paymentService.UpdatePaymentIntentAmountAsync(
                request.PaymentIntentId, calc.TotalAmount, calc.CalculationId);

            return new JsonResult(new
            {
                subtotal = calc.Subtotal,
                tax = calc.TaxAmount,
                total = calc.TotalAmount,
                calculationId = calc.CalculationId,
            });
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe tax calculation failed for intent {IntentId}", request.PaymentIntentId);
            return StatusCode(502, new { error = "Tax calculation failed. Please try again." });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CartItems = _cartService.GetCart();
        Subtotal = _cartService.GetTotal();
        Total = Subtotal;

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
            var freshIntent = await _paymentService.CreatePaymentIntentAsync(Subtotal);
            ClientSecret = freshIntent.ClientSecret;
            PaymentIntentId = freshIntent.Id;
            return Page();
        }

        var paymentIntent = await _paymentService.GetPaymentIntentAsync(PaymentIntentId);

        if (paymentIntent.Status != "succeeded")
        {
            return RedirectToPage("/Checkout/Index");
        }

        var business = _businessOptions.Value;
        var address = ResolveTaxAddress(
            new TaxRecalcRequest
            {
                PaymentIntentId = PaymentIntentId,
                FulfillmentMethod = FulfillmentMethod,
                AddressLine1 = ShippingAddress,
                City = City,
                State = State,
                ZipCode = ZipCode,
            }, business);

        decimal taxAmount = 0m;
        string? taxCalculationId = null;
        if (address is not null)
        {
            try
            {
                var calc = await _paymentService.CalculateTaxAsync(Subtotal, address);
                taxAmount = calc.TaxAmount;
                taxCalculationId = calc.CalculationId;
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogError(ex, "Tax recalculation on post failed for intent {IntentId}", PaymentIntentId);
                ModelState.AddModelError(string.Empty,
                    "We couldn't verify the tax for your order. Please try again.");
                var freshIntent = await _paymentService.CreatePaymentIntentAsync(Subtotal);
                ClientSecret = freshIntent.ClientSecret;
                PaymentIntentId = freshIntent.Id;
                return Page();
            }
        }

        Total = Subtotal + taxAmount;

        var expectedAmountInCents = (long)(Total * 100);
        if (paymentIntent.Amount != expectedAmountInCents)
        {
            ModelState.AddModelError(string.Empty,
                "Cart total has changed since payment was authorized. Please review your cart and try again.");
            var freshIntent = await _paymentService.CreatePaymentIntentAsync(Subtotal);
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
            Subtotal = Subtotal,
            TaxAmount = taxAmount,
            TotalAmount = Total,
            TaxCalculationId = taxCalculationId,
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

        var viewUrl = _orderViewTokenService.GetViewUrl(order.Id);

        try
        {
            await _emailSender.SendAsync(OrderConfirmationEmail.Build(order, business, viewUrl));
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

    private static TaxAddress? ResolveTaxAddress(TaxRecalcRequest request, BusinessSettings business)
    {
        if (request.FulfillmentMethod == FulfillmentMethod.Pickup)
        {
            var pickup = business.Pickup;
            if (string.IsNullOrWhiteSpace(pickup.AddressLine1)
                || string.IsNullOrWhiteSpace(pickup.City)
                || string.IsNullOrWhiteSpace(pickup.State)
                || string.IsNullOrWhiteSpace(pickup.PostalCode))
            {
                return null;
            }
            return new TaxAddress(
                Line1: pickup.AddressLine1!,
                Line2: pickup.AddressLine2,
                City: pickup.City!,
                State: pickup.State!,
                PostalCode: pickup.PostalCode!);
        }

        if (string.IsNullOrWhiteSpace(request.AddressLine1)
            || string.IsNullOrWhiteSpace(request.City)
            || string.IsNullOrWhiteSpace(request.State)
            || string.IsNullOrWhiteSpace(request.ZipCode))
        {
            return null;
        }

        return new TaxAddress(
            Line1: request.AddressLine1!,
            Line2: null,
            City: request.City!,
            State: request.State!,
            PostalCode: request.ZipCode!);
    }
}

public class TaxRecalcRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FulfillmentMethod FulfillmentMethod { get; set; }

    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
}
