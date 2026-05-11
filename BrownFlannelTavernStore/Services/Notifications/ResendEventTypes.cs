namespace BrownFlannelTavernStore.Services.Notifications;

public static class ResendEventTypes
{
    public const string EmailSent = "email.sent";
    public const string EmailDelivered = "email.delivered";
    public const string EmailDeliveryDelayed = "email.delivery_delayed";
    public const string EmailBounced = "email.bounced";
    public const string EmailComplained = "email.complained";
    public const string EmailOpened = "email.opened";
    public const string EmailClicked = "email.clicked";
}
