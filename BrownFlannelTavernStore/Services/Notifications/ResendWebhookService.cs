using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using Microsoft.EntityFrameworkCore;

namespace BrownFlannelTavernStore.Services.Notifications;

public class ResendWebhookService
{
    private static readonly TimeSpan SignatureToleranceWindow = TimeSpan.FromMinutes(5);

    private readonly StoreDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendWebhookService> _logger;

    public ResendWebhookService(StoreDbContext context, IConfiguration configuration, ILogger<ResendWebhookService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task HandleEventAsync(string body, string svixId, string svixTimestamp, string svixSignatureHeader, CancellationToken cancellationToken = default)
    {
        var webhookSecret = _configuration["Resend:WebhookSecret"]
            ?? throw new InvalidOperationException("Resend:WebhookSecret is not configured.");

        VerifySignature(body, svixId, svixTimestamp, svixSignatureHeader, webhookSecret);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var eventType = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
        var emailId = root.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("email_id", out var idProp)
            ? idProp.GetString()
            : null;

        if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(emailId))
        {
            _logger.LogWarning("Resend webhook payload missing type or data.email_id");
            return;
        }

        _logger.LogInformation("Received Resend webhook event {Type} for email {EmailId}", eventType, emailId);

        var newStatus = MapEventToStatus(eventType);
        if (newStatus is null)
        {
            return;
        }

        var log = await _context.EmailLogs
            .FirstOrDefaultAsync(e => e.ProviderMessageId == emailId, cancellationToken);

        if (log is null)
        {
            _logger.LogWarning(
                "Resend webhook for unknown email {EmailId} (type: {Type}). " +
                "Either the email was sent from a different system, or the EmailLog row was deleted.",
                emailId, eventType);
            return;
        }

        log.Status = newStatus.Value;
        log.DeliveryUpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated EmailLog {Id} status to {Status}", log.Id, newStatus);
    }

    private static EmailStatus? MapEventToStatus(string eventType) => eventType switch
    {
        "email.delivered" => EmailStatus.Delivered,
        "email.bounced" => EmailStatus.Bounced,
        "email.complained" => EmailStatus.Complained,
        _ => null
    };

    private static void VerifySignature(string body, string svixId, string svixTimestamp, string signatureHeader, string secret)
    {
        if (string.IsNullOrEmpty(svixId) || string.IsNullOrEmpty(svixTimestamp) || string.IsNullOrEmpty(signatureHeader))
        {
            throw new UnauthorizedAccessException("Missing required Svix headers.");
        }

        if (!long.TryParse(svixTimestamp, out var unixTimestamp))
        {
            throw new UnauthorizedAccessException("svix-timestamp is not a valid integer.");
        }

        var eventTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        var age = DateTimeOffset.UtcNow - eventTime;
        if (age > SignatureToleranceWindow || age < -SignatureToleranceWindow)
        {
            throw new UnauthorizedAccessException("svix-timestamp is outside the allowed tolerance window.");
        }

        var secretBody = secret.StartsWith("whsec_") ? secret[6..] : secret;
        var secretBytes = Convert.FromBase64String(secretBody);

        var signedPayload = $"{svixId}.{svixTimestamp}.{body}";
        using var hmac = new HMACSHA256(secretBytes);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var computedSignature = Convert.ToBase64String(computedHash);

        var providedSignatures = signatureHeader
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.StartsWith("v1,"))
            .Select(s => s["v1,".Length..]);

        var match = providedSignatures.Any(sig => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(sig),
            Encoding.UTF8.GetBytes(computedSignature)));

        if (!match)
        {
            throw new UnauthorizedAccessException("Resend webhook signature verification failed.");
        }
    }
}
