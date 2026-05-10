namespace BrownFlannelTavernStore.Services.Notifications;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
