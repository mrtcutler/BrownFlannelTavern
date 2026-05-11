using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace BrownFlannelTavernStore.Services;

public class StripeWebhookService
{
    private readonly StoreDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(StoreDbContext context, IConfiguration configuration, ILogger<StripeWebhookService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task HandleEventAsync(string json, string signature)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"]
            ?? throw new InvalidOperationException("Stripe:WebhookSecret not configured");

        var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

        _logger.LogInformation("Received Stripe webhook event {Type} (id {Id})", stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case StripeEventTypes.PaymentIntentSucceeded:
                await HandlePaymentIntentSucceededAsync((PaymentIntent)stripeEvent.Data.Object);
                break;
            default:
                _logger.LogInformation("Ignoring unhandled Stripe event type {Type}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandlePaymentIntentSucceededAsync(PaymentIntent paymentIntent)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntent.Id);

        if (order == null)
        {
            _logger.LogWarning(
                "payment_intent.succeeded for {PaymentIntentId} arrived with no matching order in DB. " +
                "Customer likely closed the browser before form submission completed; manual reconciliation may be needed.",
                paymentIntent.Id);
            return;
        }

        if (order.Status != OrderStatus.Paid)
        {
            order.Status = OrderStatus.Paid;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Marked order {OrderId} as Paid via webhook", order.Id);
        }
    }
}
