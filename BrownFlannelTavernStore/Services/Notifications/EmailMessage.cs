using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Services.Notifications;

public record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    EmailType EmailType,
    string? TextBody = null,
    int? OrderId = null,
    string? UserId = null);
