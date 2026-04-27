using Stripe;
using Stripe.V2;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Services;

public class PaymentService
{
    private readonly IConfiguration _configuration;

    public PaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<Stripe.PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
    {
        var options = new Stripe.PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100), // Stripe uses cents
            Currency = currency,
            AutomaticPaymentMethods = new Stripe.PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
        };

        var service = new Stripe.PaymentIntentService();
        return await service.CreateAsync(options);
    }

    public async Task<Stripe.PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
    {
        var service = new Stripe.PaymentIntentService();
        return await service.GetAsync(paymentIntentId);
    }
}
