using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Models.Settings;
using BrownFlannelTavernStore.Pages.Orders;
using BrownFlannelTavernStore.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrownFlannelTavernStore.Tests.Pages.Orders;

public class ViewModelTests
{
    private static StoreDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options);

    private static OrderViewTokenService BuildTokenService()
    {
        var settings = new OrderViewSettings
        {
            Secret = "test-secret-that-is-at-least-32-characters-long-for-hmac",
            BaseUrl = "https://example.com",
            ExpiryDays = 90,
        };
        return new OrderViewTokenService(Options.Create(settings));
    }

    private static async Task<Order> SeedOrder(StoreDbContext db)
    {
        var order = new Order
        {
            CustomerEmail = "customer@example.com",
            CustomerName = "Jane Doe",
            Status = OrderStatus.Paid,
            Subtotal = 24.99m,
            TaxAmount = 1.50m,
            TotalAmount = 26.49m,
            StripePaymentIntentId = "pi_test",
            Lines = [new OrderLine { ProductName = "Sticker", Quantity = 1, UnitPrice = 24.99m }]
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task OnGetAsync_ValidToken_LoadsOrder()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrder(db);
        var tokenService = BuildTokenService();
        var token = tokenService.GenerateToken(order.Id);
        var page = new ViewModel(db, tokenService);

        await page.OnGetAsync(token);

        page.TokenInvalid.Should().BeFalse();
        page.Order.Should().NotBeNull();
        page.Order!.Id.Should().Be(order.Id);
        page.Order.Lines.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-token")]
    public async Task OnGetAsync_MissingOrInvalidToken_SetsTokenInvalid(string? token)
    {
        await using var db = NewInMemoryDb();
        await SeedOrder(db);
        var page = new ViewModel(db, BuildTokenService());

        await page.OnGetAsync(token);

        page.TokenInvalid.Should().BeTrue();
        page.Order.Should().BeNull();
    }

    [Fact]
    public async Task OnGetAsync_TokenForMissingOrder_SetsTokenInvalid()
    {
        await using var db = NewInMemoryDb();
        var tokenService = BuildTokenService();
        var token = tokenService.GenerateToken(orderId: 99999);
        var page = new ViewModel(db, tokenService);

        await page.OnGetAsync(token);

        page.TokenInvalid.Should().BeTrue();
        page.Order.Should().BeNull();
    }

    [Fact]
    public async Task OnGetAsync_TokenSignedWithDifferentSecret_SetsTokenInvalid()
    {
        await using var db = NewInMemoryDb();
        var order = await SeedOrder(db);

        var attackerSettings = new OrderViewSettings
        {
            Secret = "attacker-secret-trying-to-forge-tokens-with-wrong-key",
            BaseUrl = "https://example.com",
            ExpiryDays = 90,
        };
        var attackerService = new OrderViewTokenService(Options.Create(attackerSettings));
        var forgedToken = attackerService.GenerateToken(order.Id);

        var page = new ViewModel(db, BuildTokenService());
        await page.OnGetAsync(forgedToken);

        page.TokenInvalid.Should().BeTrue();
        page.Order.Should().BeNull();
    }
}
