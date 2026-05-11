using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Pages.Admin.Orders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BrownFlannelTavernStore.Tests.Pages.Admin.Orders;

public class IndexModelTests
{
    private static StoreDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options);

    private static async Task SeedOrders(StoreDbContext db, int count, OrderStatus status = OrderStatus.Paid)
    {
        for (var i = 1; i <= count; i++)
        {
            db.Orders.Add(new Order
            {
                CustomerEmail = $"c{i}@example.com",
                CustomerName = $"Customer {i}",
                Status = status,
                TotalAmount = 10m,
                StripePaymentIntentId = $"pi_{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task OnGetAsync_NoParams_DefaultsToFirstPage()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 30);
        var page = new IndexModel(db);

        await page.OnGetAsync(status: null, page: 1);

        page.Orders.Page.Should().Be(1);
        page.Orders.PageSize.Should().Be(25);
        page.Orders.Items.Count.Should().Be(25);
        page.Orders.TotalCount.Should().Be(30);
        page.Orders.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task OnGetAsync_Page2_ReturnsSecondSlice()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 30);
        var page = new IndexModel(db);

        await page.OnGetAsync(status: null, page: 2);

        page.Orders.Items.Count.Should().Be(5);
        page.Orders.Page.Should().Be(2);
    }

    [Fact]
    public async Task OnGetAsync_StatusFilter_LimitsToMatchingOrdersAndPreservesInPagination()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 30, OrderStatus.Paid);
        await SeedOrders(db, 5, OrderStatus.Cancelled);
        var page = new IndexModel(db);

        await page.OnGetAsync(status: OrderStatus.Cancelled, page: 1);

        page.Orders.TotalCount.Should().Be(5);
        page.Orders.Items.Should().OnlyContain(o => o.Status == OrderStatus.Cancelled);
        page.StatusFilter.Should().Be(OrderStatus.Cancelled);
        page.Pagination.RouteData.Should().ContainKey("status").WhoseValue.Should().Be("Cancelled");
    }
}
