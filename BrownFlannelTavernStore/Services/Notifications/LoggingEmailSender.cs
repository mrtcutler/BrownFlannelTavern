using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Services.Notifications;

public class LoggingEmailSender : IEmailSender
{
    private readonly ResendEmailSender _inner;
    private readonly StoreDbContext _db;
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ResendEmailSender inner, StoreDbContext db, ILogger<LoggingEmailSender> logger)
    {
        _inner = inner;
        _db = db;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var log = new EmailLog
        {
            ToAddress = message.To,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
            EmailType = message.EmailType,
            OrderId = message.OrderId,
            UserId = message.UserId,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var result = await _inner.SendAsync(message, cancellationToken);
            log.Status = EmailStatus.Sent;
            log.ProviderMessageId = result.ProviderMessageId;
            return result;
        }
        catch (Exception ex)
        {
            log.Status = EmailStatus.Failed;
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Email send failed (to: {To}, type: {Type})", message.To, message.EmailType);
            throw;
        }
        finally
        {
            _db.EmailLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
