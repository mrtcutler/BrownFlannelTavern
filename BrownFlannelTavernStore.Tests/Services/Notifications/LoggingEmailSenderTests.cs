using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BrownFlannelTavernStore.Tests.Services.Notifications;

public class LoggingEmailSenderTests
{
    private static StoreDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options);

    private static Mock<ResendEmailSender> NewMockInnerSender() =>
        new(new HttpClient(),
            new ConfigurationBuilder().Build(),
            Mock.Of<ILogger<ResendEmailSender>>());

    private static EmailMessage SampleMessage(EmailType type = EmailType.OrderConfirmation, int? orderId = null, string? userId = null) =>
        new(To: "customer@example.com",
            Subject: "Order confirmation",
            HtmlBody: "<p>Thanks</p>",
            EmailType: type,
            TextBody: "Thanks",
            OrderId: orderId,
            UserId: userId);

    [Fact]
    public async Task SendAsync_SuccessfulInnerSend_WritesSentLogAndReturnsResult()
    {
        await using var db = NewInMemoryDb();
        var mockInner = NewMockInnerSender();
        mockInner.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new EmailSendResult("provider_msg_123"));
        var sender = new LoggingEmailSender(mockInner.Object, db, Mock.Of<ILogger<LoggingEmailSender>>());

        var result = await sender.SendAsync(SampleMessage(orderId: 7, userId: "user-abc"));

        result.ProviderMessageId.Should().Be("provider_msg_123");
        var log = await db.EmailLogs.SingleAsync();
        log.Status.Should().Be(EmailStatus.Sent);
        log.ProviderMessageId.Should().Be("provider_msg_123");
        log.ToAddress.Should().Be("customer@example.com");
        log.Subject.Should().Be("Order confirmation");
        log.EmailType.Should().Be(EmailType.OrderConfirmation);
        log.OrderId.Should().Be(7);
        log.UserId.Should().Be("user-abc");
        log.HtmlBody.Should().Be("<p>Thanks</p>");
        log.TextBody.Should().Be("Thanks");
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_InnerThrows_WritesFailedLogAndRethrows()
    {
        await using var db = NewInMemoryDb();
        var mockInner = NewMockInnerSender();
        mockInner.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Resend API rejected: 422 Invalid recipient"));
        var sender = new LoggingEmailSender(mockInner.Object, db, Mock.Of<ILogger<LoggingEmailSender>>());

        var act = () => sender.SendAsync(SampleMessage());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid recipient*");

        var log = await db.EmailLogs.SingleAsync();
        log.Status.Should().Be(EmailStatus.Failed);
        log.ProviderMessageId.Should().BeNull();
        log.ErrorMessage.Should().Contain("Invalid recipient");
        log.HtmlBody.Should().Be("<p>Thanks</p>");
    }

    [Fact]
    public async Task SendAsync_OptionalFieldsNull_LogsWithNulls()
    {
        await using var db = NewInMemoryDb();
        var mockInner = NewMockInnerSender();
        mockInner.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new EmailSendResult("x"));
        var sender = new LoggingEmailSender(mockInner.Object, db, Mock.Of<ILogger<LoggingEmailSender>>());

        await sender.SendAsync(new EmailMessage(
            To: "a@b.c",
            Subject: "S",
            HtmlBody: "<p>x</p>",
            EmailType: EmailType.AdminAlert));

        var log = await db.EmailLogs.SingleAsync();
        log.OrderId.Should().BeNull();
        log.UserId.Should().BeNull();
        log.TextBody.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_SetsCreatedAtToCurrentUtc()
    {
        await using var db = NewInMemoryDb();
        var mockInner = NewMockInnerSender();
        mockInner.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new EmailSendResult("x"));
        var sender = new LoggingEmailSender(mockInner.Object, db, Mock.Of<ILogger<LoggingEmailSender>>());

        var before = DateTime.UtcNow;
        await sender.SendAsync(SampleMessage());
        var after = DateTime.UtcNow;

        var log = await db.EmailLogs.SingleAsync();
        log.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
