namespace BrownFlannelTavernStore.Services.Notifications;

public record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TextBody = null);
