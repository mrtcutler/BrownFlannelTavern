namespace BrownFlannelTavernStore.Services;

public static class StripeEventTypes
{
    public const string PaymentIntentSucceeded = "payment_intent.succeeded";
    public const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
    public const string ChargeRefunded = "charge.refunded";
}
