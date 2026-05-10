using System.Security.Cryptography;
using System.Text;
using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BrownFlannelTavernStore.Tests.Services.Notifications;

public class ResendWebhookServiceTests
{
    private const string TestSecret = "whsec_dGVzdHNlY3JldGZvcndlYmhvb2tzMTIzNDU2Nzg=";

    private static StoreDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options);

    private static IConfiguration BuildConfig(string? webhookSecret = TestSecret)
    {
        var dict = new Dictionary<string, string?>();
        if (webhookSecret != null) dict["Resend:WebhookSecret"] = webhookSecret;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static ResendWebhookService BuildService(StoreDbContext db, IConfiguration? config = null) =>
        new(db, config ?? BuildConfig(), Mock.Of<ILogger<ResendWebhookService>>());

    private static (string body, string id, string timestamp, string signatureHeader) BuildSignedRequest(
        string body, string secret = TestSecret, long? unixTimestamp = null)
    {
        var id = "msg_test_" + Guid.NewGuid().ToString("N");
        var timestamp = (unixTimestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString();

        var secretBytes = Convert.FromBase64String(secret["whsec_".Length..]);
        var signedPayload = $"{id}.{timestamp}.{body}";
        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        return (body, id, timestamp, $"v1,{Convert.ToBase64String(hash)}");
    }

    private static async Task<EmailLog> SeedSentLog(StoreDbContext db, string providerMessageId)
    {
        var log = new EmailLog
        {
            ToAddress = "test@example.com",
            Subject = "Test",
            EmailType = EmailType.OrderConfirmation,
            HtmlBody = "<p>x</p>",
            Status = EmailStatus.Sent,
            ProviderMessageId = providerMessageId
        };
        db.EmailLogs.Add(log);
        await db.SaveChangesAsync();
        return log;
    }

    [Fact]
    public async Task HandleEventAsync_DeliveredEvent_UpdatesStatusToDelivered()
    {
        await using var db = NewInMemoryDb();
        await SeedSentLog(db, "email_abc");
        var service = BuildService(db);
        var json = """{"type":"email.delivered","data":{"email_id":"email_abc"}}""";
        var (body, id, ts, sig) = BuildSignedRequest(json);

        await service.HandleEventAsync(body, id, ts, sig);

        var log = await db.EmailLogs.SingleAsync();
        log.Status.Should().Be(EmailStatus.Delivered);
        log.DeliveryUpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleEventAsync_BouncedEvent_UpdatesStatusToBounced()
    {
        await using var db = NewInMemoryDb();
        await SeedSentLog(db, "email_xyz");
        var service = BuildService(db);
        var json = """{"type":"email.bounced","data":{"email_id":"email_xyz"}}""";
        var (body, id, ts, sig) = BuildSignedRequest(json);

        await service.HandleEventAsync(body, id, ts, sig);

        var log = await db.EmailLogs.SingleAsync();
        log.Status.Should().Be(EmailStatus.Bounced);
    }

    [Fact]
    public async Task HandleEventAsync_ComplainedEvent_UpdatesStatusToComplained()
    {
        await using var db = NewInMemoryDb();
        await SeedSentLog(db, "email_spam");
        var service = BuildService(db);
        var json = """{"type":"email.complained","data":{"email_id":"email_spam"}}""";
        var (body, id, ts, sig) = BuildSignedRequest(json);

        await service.HandleEventAsync(body, id, ts, sig);

        var log = await db.EmailLogs.SingleAsync();
        log.Status.Should().Be(EmailStatus.Complained);
    }

    [Fact]
    public async Task HandleEventAsync_UnknownEventType_DoesNotChangeStatus()
    {
        await using var db = NewInMemoryDb();
        await SeedSentLog(db, "email_open");
        var service = BuildService(db);
        var json = """{"type":"email.opened","data":{"email_id":"email_open"}}""";
        var (body, id, ts, sig) = BuildSignedRequest(json);

        await service.HandleEventAsync(body, id, ts, sig);

        var log = await db.EmailLogs.SingleAsync();
        log.Status.Should().Be(EmailStatus.Sent);
        log.DeliveryUpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task HandleEventAsync_UnknownEmailId_LogsWarningAndDoesNotThrow()
    {
        await using var db = NewInMemoryDb();
        var service = BuildService(db);
        var json = """{"type":"email.delivered","data":{"email_id":"email_does_not_exist"}}""";
        var (body, id, ts, sig) = BuildSignedRequest(json);

        var act = () => service.HandleEventAsync(body, id, ts, sig);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleEventAsync_InvalidSignature_Throws()
    {
        await using var db = NewInMemoryDb();
        await SeedSentLog(db, "email_abc");
        var service = BuildService(db);
        var json = """{"type":"email.delivered","data":{"email_id":"email_abc"}}""";
        var (body, id, ts, _) = BuildSignedRequest(json);

        var act = () => service.HandleEventAsync(body, id, ts, "v1,bogussignature");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task HandleEventAsync_OldTimestamp_ThrowsForReplayProtection()
    {
        await using var db = NewInMemoryDb();
        var service = BuildService(db);
        var json = """{"type":"email.delivered","data":{"email_id":"email_abc"}}""";
        var oldTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var (body, id, ts, sig) = BuildSignedRequest(json, unixTimestamp: oldTimestamp);

        var act = () => service.HandleEventAsync(body, id, ts, sig);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*tolerance*");
    }

    [Fact]
    public async Task HandleEventAsync_MissingSvixHeaders_Throws()
    {
        await using var db = NewInMemoryDb();
        var service = BuildService(db);
        var json = """{"type":"email.delivered","data":{"email_id":"x"}}""";

        var act = () => service.HandleEventAsync(json, "", "", "");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
