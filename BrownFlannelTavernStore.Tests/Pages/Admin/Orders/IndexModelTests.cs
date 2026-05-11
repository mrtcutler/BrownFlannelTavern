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
                TotalAmount = i * 10m,
                StripePaymentIntentId = $"pi_{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task OnGetAsync_NoParams_DefaultsToFirstPageSortedByDateDesc()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 30);
        var page = new IndexModel(db);

        await page.OnGetAsync(1);

        page.Orders.Page.Should().Be(1);
        page.Orders.Items.Count.Should().Be(25);
        page.Orders.TotalCount.Should().Be(30);
        page.Orders.Items.First().CustomerName.Should().Be("Customer 1");
    }

    [Fact]
    public async Task OnGetAsync_Page2_ReturnsSecondSlice()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 30);
        var page = new IndexModel(db);

        await page.OnGetAsync(2);

        page.Orders.Items.Count.Should().Be(5);
        page.Orders.Page.Should().Be(2);
    }

    [Fact]
    public async Task OnGetAsync_StatusFilter_LimitsAndPreservesInRouteData()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 30, OrderStatus.Paid);
        await SeedOrders(db, 5, OrderStatus.Cancelled);
        var page = new IndexModel(db) { StatusFilter = OrderStatus.Cancelled };

        await page.OnGetAsync(1);

        page.Orders.TotalCount.Should().Be(5);
        page.Orders.Items.Should().OnlyContain(o => o.Status == OrderStatus.Cancelled);
        page.Pagination.RouteData["StatusFilter"].Should().Be("Cancelled");
    }

    [Fact]
    public async Task OnGetAsync_OrderIdFilter_ReturnsExactMatch()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 10);
        var targetId = (await db.Orders.FirstAsync()).Id;
        var page = new IndexModel(db) { OrderIdFilter = targetId };

        await page.OnGetAsync(1);

        page.Orders.TotalCount.Should().Be(1);
        page.Orders.Items.Single().Id.Should().Be(targetId);
    }

    [Fact]
    public async Task OnGetAsync_CustomerSearch_MatchesNameOrEmail()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 10);
        var page = new IndexModel(db) { CustomerSearchFilter = "Customer 5" };

        await page.OnGetAsync(1);

        page.Orders.TotalCount.Should().Be(1);
        page.Orders.Items.Single().CustomerName.Should().Be("Customer 5");
    }

    [Fact]
    public async Task OnGetAsync_DateRange_FiltersByCreatedAt()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 10);
        var page = new IndexModel(db)
        {
            DateFromFilter = DateTime.UtcNow.AddDays(-1),
            DateToFilter = DateTime.UtcNow
        };

        await page.OnGetAsync(1);

        page.Orders.Items.Should().AllSatisfy(o => o.CreatedAt.Should().BeAfter(DateTime.UtcNow.AddDays(-1).AddSeconds(-1)));
    }

    [Fact]
    public async Task OnGetAsync_SortByTotalAsc_OrdersAscending()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 5);
        var page = new IndexModel(db) { SortBy = "total", SortDir = "asc" };

        await page.OnGetAsync(1);

        var totals = page.Orders.Items.Select(o => o.TotalAmount).ToList();
        totals.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task OnGetAsync_SortByTotalDesc_OrdersDescending()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 5);
        var page = new IndexModel(db) { SortBy = "total", SortDir = "desc" };

        await page.OnGetAsync(1);

        var totals = page.Orders.Items.Select(o => o.TotalAmount).ToList();
        totals.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task OnGetAsync_UnknownSortBy_FallsBackToDateDesc()
    {
        await using var db = NewInMemoryDb();
        await SeedOrders(db, 5);
        var page = new IndexModel(db) { SortBy = "nonexistent" };

        await page.OnGetAsync(1);

        var dates = page.Orders.Items.Select(o => o.CreatedAt).ToList();
        dates.Should().BeInDescendingOrder();
    }
}
