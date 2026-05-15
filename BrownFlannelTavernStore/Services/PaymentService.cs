using Stripe;
using Stripe.Tax;

namespace BrownFlannelTavernStore.Services;

public class PaymentService
{
    // Stripe Tax product code "General - Tangible Goods" — the catch-all for physical merchandise.
    // Works for apparel, accessories, stickers, etc. MI doesn't exempt clothing, so a single
    // general code covers the whole catalog at the same rate. If a future product needs a
    // different tax treatment (e.g. food, digital goods), promote tax_code to a per-variant field.
    public const string DefaultTaxCode = "txcd_99999999";

    private readonly IConfiguration _configuration;

    public PaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = currency,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
        };

        var service = new PaymentIntentService();
        return await service.CreateAsync(options);
    }

    public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
    {
        var service = new PaymentIntentService();
        return await service.GetAsync(paymentIntentId);
    }

    public async Task<PaymentIntent> UpdatePaymentIntentAmountAsync(
        string paymentIntentId, decimal newAmount, string? taxCalculationId = null)
    {
        var options = new PaymentIntentUpdateOptions
        {
            Amount = (long)(newAmount * 100),
        };
        if (!string.IsNullOrWhiteSpace(taxCalculationId))
        {
            options.Metadata = new Dictionary<string, string>
            {
                ["tax_calculation_id"] = taxCalculationId,
            };
        }

        var service = new PaymentIntentService();
        return await service.UpdateAsync(paymentIntentId, options);
    }

    public async Task<Refund> CreateRefundAsync(string paymentIntentId)
    {
        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
        };
        var service = new RefundService();
        return await service.CreateAsync(options);
    }

    public async Task<TaxCalculationResult> CalculateTaxAsync(
        decimal subtotal,
        TaxAddress address,
        string currency = "usd")
    {
        var options = new CalculationCreateOptions
        {
            Currency = currency,
            LineItems =
            [
                new CalculationLineItemOptions
                {
                    Amount = (long)(subtotal * 100),
                    Reference = "cart-subtotal",
                    TaxCode = DefaultTaxCode,
                    TaxBehavior = "exclusive",
                }
            ],
            CustomerDetails = new CalculationCustomerDetailsOptions
            {
                Address = new AddressOptions
                {
                    Line1 = address.Line1,
                    Line2 = address.Line2,
                    City = address.City,
                    State = address.State,
                    PostalCode = address.PostalCode,
                    Country = address.Country,
                },
                AddressSource = "shipping",
            },
        };

        var service = new CalculationService();
        var calc = await service.CreateAsync(options);

        return new TaxCalculationResult(
            Subtotal: subtotal,
            TaxAmount: calc.TaxAmountExclusive / 100m,
            TotalAmount: calc.AmountTotal / 100m,
            CalculationId: calc.Id);
    }
}

public record TaxAddress(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country = "US");

public record TaxCalculationResult(
    decimal Subtotal,
    decimal TaxAmount,
    decimal TotalAmount,
    string CalculationId);
