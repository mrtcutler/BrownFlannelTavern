using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Pages.Admin.Orders;
using BrownFlannelTavernStore.Services.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

    private static DetailsModel BuildPage(StoreDbContext db, Mock<IEmailSender>? mockSender = null)
    {
        mockSender ??= new Mock<IEmailSender>();
        mockSender.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new EmailSendResult("x"));
        return new DetailsModel(db, mockSender.Object, Mock.Of<ILogger<DetailsModel>>());
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
}
