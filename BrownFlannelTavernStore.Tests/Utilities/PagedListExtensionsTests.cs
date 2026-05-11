using BrownFlannelTavernStore.Data;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BrownFlannelTavernStore.Tests.Utilities;

public class PagedListExtensionsTests
{
    private static StoreDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options);

    private static async Task<StoreDbContext> SeedOrders(int count)
    {
        var db = NewInMemoryDb();
        for (var i = 1; i <= count; i++)
        {
            db.Orders.Add(new Order
            {
                CustomerEmail = $"c{i}@example.com",
                CustomerName = $"Customer {i:D4}",
                Status = OrderStatus.Paid,
                TotalAmount = 10m,
                StripePaymentIntentId = $"pi_{i}"
            });
        }
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task ToPagedListAsync_FirstPage_ReturnsFirstSlice()
    {
        await using var db = await SeedOrders(60);

        var result = await db.Orders.OrderBy(o => o.CustomerName).ToPagedListAsync(page: 1, pageSize: 25);

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(25);
        result.TotalCount.Should().Be(60);
        result.TotalPages.Should().Be(3);
        result.Items.Count.Should().Be(25);
        result.Items.First().CustomerName.Should().Be("Customer 0001");
    }

    [Fact]
    public async Task ToPagedListAsync_MiddlePage_ReturnsMiddleSlice()
    {
        await using var db = await SeedOrders(60);

        var result = await db.Orders.OrderBy(o => o.CustomerName).ToPagedListAsync(page: 2, pageSize: 25);

        result.Items.Count.Should().Be(25);
        result.Items.First().CustomerName.Should().Be("Customer 0026");
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task ToPagedListAsync_LastPage_ReturnsPartialSlice()
    {
        await using var db = await SeedOrders(60);

        var result = await db.Orders.OrderBy(o => o.CustomerName).ToPagedListAsync(page: 3, pageSize: 25);

        result.Items.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToPagedListAsync_PageBeyondTotal_ReturnsEmpty()
    {
        await using var db = await SeedOrders(60);

        var result = await db.Orders.OrderBy(o => o.CustomerName).ToPagedListAsync(page: 99, pageSize: 25);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(60);
    }

    [Fact]
    public async Task ToPagedListAsync_InvalidPage_ClampsToOne()
    {
        await using var db = await SeedOrders(10);

        var result = await db.Orders.OrderBy(o => o.CustomerName).ToPagedListAsync(page: 0, pageSize: 25);

        result.Page.Should().Be(1);
        result.Items.Count.Should().Be(10);
    }

    [Fact]
    public async Task ToPagedListAsync_InvalidPageSize_UsesDefault()
    {
        await using var db = await SeedOrders(50);

        var result = await db.Orders.OrderBy(o => o.CustomerName).ToPagedListAsync(page: 1, pageSize: 0);

        result.PageSize.Should().Be(PagedListExtensions.DefaultPageSize);
    }

    [Fact]
    public async Task ToPagedListAsync_EmptyQuery_ReturnsEmptyPagedList()
    {
        await using var db = NewInMemoryDb();

        var result = await db.Orders.OrderBy(o => o.Id).ToPagedListAsync(page: 1, pageSize: 25);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasPrevious.Should().BeFalse();
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public void ToPagedList_InMemorySource_PaginatesCorrectly()
    {
        var list = Enumerable.Range(1, 50).ToList();

        var result = list.ToPagedList(page: 2, pageSize: 10);

        result.Items.Should().BeEquivalentTo(Enumerable.Range(11, 10));
        result.TotalCount.Should().Be(50);
        result.TotalPages.Should().Be(5);
    }

    [Theory]
    [InlineData(0, 25, 0)]
    [InlineData(1, 25, 1)]
    [InlineData(25, 25, 1)]
    [InlineData(26, 25, 2)]
    [InlineData(100, 25, 4)]
    [InlineData(101, 25, 5)]
    public async Task ToPagedListAsync_TotalPagesMath_IsCorrect(int totalItems, int pageSize, int expectedTotalPages)
    {
        await using var db = await SeedOrders(totalItems);

        var result = await db.Orders.OrderBy(o => o.Id).ToPagedListAsync(page: 1, pageSize: pageSize);

        result.TotalPages.Should().Be(expectedTotalPages);
    }

    [Fact]
    public async Task PagedList_HasPreviousHasNext_ReflectPosition()
    {
        await using var db = await SeedOrders(60);

        var firstPage = await db.Orders.OrderBy(o => o.Id).ToPagedListAsync(page: 1, pageSize: 25);
        firstPage.HasPrevious.Should().BeFalse();
        firstPage.HasNext.Should().BeTrue();

        var middlePage = await db.Orders.OrderBy(o => o.Id).ToPagedListAsync(page: 2, pageSize: 25);
        middlePage.HasPrevious.Should().BeTrue();
        middlePage.HasNext.Should().BeTrue();

        var lastPage = await db.Orders.OrderBy(o => o.Id).ToPagedListAsync(page: 3, pageSize: 25);
        lastPage.HasPrevious.Should().BeTrue();
        lastPage.HasNext.Should().BeFalse();
    }
}
