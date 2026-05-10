using BrownFlannelTavernStore.Models;
using FluentAssertions;

namespace BrownFlannelTavernStore.Tests.Models;

public class CartItemTests
{
    [Fact]
    public void Total_MultipliesPriceAndQuantity()
    {
        var item = new CartItem { Price = 10.50m, Quantity = 3 };

        item.Total.Should().Be(31.50m);
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(19.99, 1, 19.99)]
    [InlineData(24.99, 2, 49.98)]
    public void Total_HandlesEdgeCases(decimal price, int quantity, decimal expected)
    {
        var item = new CartItem { Price = price, Quantity = quantity };

        item.Total.Should().Be(expected);
    }
}
