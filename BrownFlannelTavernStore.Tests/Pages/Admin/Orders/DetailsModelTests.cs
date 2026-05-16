using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Models.Settings;
using BrownFlannelTavernStore.Pages.Admin.Orders;
using BrownFlannelTavernStore.Services;
using BrownFlannelTavernStore.Services.Notifications;
using BrownFlannelTavernStore.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BrownFlannelTavernStore.Tests.Pages.Admin.Orders;

public class DetailsModelTests
{
    private static StoreDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options);

    private static async Task<Order> SeedOrder(StoreDbContext db, OrderStatus status = OrderStatus.Paid)
    {
        var order = new Order
        {
            CustomerEmail = "customer@example.com",
            CustomerName = "Jane Doe",
            Status = status,
            TotalAmount = 24.99m,
            StripePaymentIntentId = "pi_test"
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    private static PaymentService StubPaymentService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Stripe:SecretKey"] = "sk_test_stub" })
            .Build();
        return new PaymentService(config);
    }

    private static OrderViewTokenService StubTokenService()
    {
        var settings = new OrderViewSettings
        {
            Secret = "test-secret-that-is-at-least-32-characters-long-for-hmac",
            BaseUrl = "https://example.com",
            ExpiryDays = 90,
        };
        return new OrderViewTokenService(Options.Create(settings));
    }

    private static DetailsModel BuildPage(StoreDbContext db, Mock<IEmailSender>? mockSender = null)
    {
        mockSender ??= new Mock<IEmailSender>();
        mockSender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new EmailSendResult("x"));
        return new DetailsModel(
            db,
            mockSender.Object,
            Options.Create(TestBusiness.Default()),
            StubPaymentService(),
            StubTokenService(),
            Mock.Of<ILogger<DetailsModel>>());
    }

    [Fact]
    public async Task OnPostAsync_StatusUnchanged_DoesNotFireEmailAndShowsSavedMessage()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrder(db, OrderStatus.Paid);
        var mockSender = new Mock<IEmailSender>();
        var page = BuildPage(db, mockSender);
        page.NewStatus = OrderStatus.Paid;
        page.OrderNotes = "a note was added";

        await page.OnPostAsync(order.Id);

        mockSender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        page.Message.Should().Be("Order saved.");
    }

    [Fact]
    public async Task OnPostAsync_StatusChanged_FiresEmailAndShowsStatusMessage()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrder(db, OrderStatus.Paid);
        var mockSender = new Mock<IEmailSender>();
        var page = BuildPage(db, mockSender);
        page.NewStatus = OrderStatus.Shipped;

        await page.OnPostAsync(order.Id);

        mockSender.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
        page.Message.Should().Be("Order status updated to Shipped.");
    }

    [Fact]
    public async Task OnPostAsync_StatusChanged_PersistsNewStatus()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrder(db, OrderStatus.Paid);
        var page = BuildPage(db);
        page.NewStatus = OrderStatus.Processing;

        await page.OnPostAsync(order.Id);

        var reloaded = await db.Orders.FirstAsync(o => o.Id == order.Id);
        reloaded.Status.Should().Be(OrderStatus.Processing);
        reloaded.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostAsync_OrderNotFound_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var page = BuildPage(db);
        page.NewStatus = OrderStatus.Paid;

        var result = await page.OnPostAsync(99999);

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_StatusSetToRefunded_RejectsAndKeepsExistingStatus()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrder(db, OrderStatus.Paid);
        var page = BuildPage(db);
        page.NewStatus = OrderStatus.Refunded;

        await page.OnPostAsync(order.Id);

        page.ErrorMessage.Should().Contain("Use the Refund button");
        var reloaded = await db.Orders.FirstAsync(o => o.Id == order.Id);
        reloaded.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task OnPostRefundAsync_OrderNotFound_ReturnsNotFound()
    {
        await using var db = NewInMemoryDb();
        var page = BuildPage(db);

        var result = await page.OnPostRefundAsync(99999);

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRefundAsync_AlreadyRefunded_SurfacesErrorAndDoesNotChangeOrder()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrder(db, OrderStatus.Refunded);
        order.RefundedAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        var page = BuildPage(db);

        await page.OnPostRefundAsync(order.Id);

        page.ErrorMessage.Should().Contain("already been refunded");
        page.Message.Should().BeNull();
        var reloaded = await db.Orders.FirstAsync(o => o.Id == order.Id);
        reloaded.Status.Should().Be(OrderStatus.Refunded);
    }

    [Fact]
    public async Task OnPostRefundAsync_NoStripePaymentIntent_SurfacesError()
    {
        await using var db = NewInMemoryDb();
        var order = new Order
        {
            CustomerEmail = "x@y.z",
            CustomerName = "No Stripe",
            Status = OrderStatus.Paid,
            TotalAmount = 10m,
            StripePaymentIntentId = ""
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var page = BuildPage(db);

        await page.OnPostRefundAsync(order.Id);

        page.ErrorMessage.Should().Contain("no Stripe payment");
        var reloaded = await db.Orders.FirstAsync(o => o.Id == order.Id);
        reloaded.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public void ManuallyAssignableStatuses_ExcludesRefunded()
    {
        DetailsModel.ManuallyAssignableStatuses
            .Should().NotContain(OrderStatus.Refunded)
            .And.Contain(OrderStatus.Paid)
            .And.Contain(OrderStatus.Cancelled);
    }
}
