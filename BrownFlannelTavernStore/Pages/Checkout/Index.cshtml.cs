using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services;

namespace BrownFlannelTavernStore.Pages.Checkout;

public class IndexModel : PageModel
{
    private readonly CartService _cartService;
    private readonly PaymentService _paymentService;
    private readonly StoreDbContext _context;
    private readonly IConfiguration _configuration;

    public IndexModel(CartService cartService, PaymentService paymentService,
        StoreDbContext context, IConfiguration configuration)
    {
        _cartService = cartService;
        _paymentService = paymentService;
        _context = context;
        _configuration = configuration;
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
    public string ShippingAddress { get; set; } = string.Empty;

    [BindProperty]
    public string City { get; set; } = string.Empty;

    [BindProperty]
    public string State { get; set; } = string.Empty;

    [BindProperty]
    public string ZipCode { get; set; } = string.Empty;

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

        // Verify payment with Stripe
        var paymentIntent = await _paymentService.GetPaymentIntentAsync(PaymentIntentId);

        if (paymentIntent.Status != "succeeded")
        {
            return RedirectToPage("/Checkout/Index");
        }

        // Create the order
        var order = new Order
        {
            StripePaymentIntentId = PaymentIntentId,
            CustomerEmail = CustomerEmail,
            CustomerName = CustomerName,
            ShippingAddress = ShippingAddress,
            City = City,
            State = State,
            ZipCode = ZipCode,
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

        _cartService.ClearCart();

        return RedirectToPage("/Orders/Confirmation", new { id = order.Id });
    }
}
