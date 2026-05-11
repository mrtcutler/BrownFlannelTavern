using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services.Notifications;
using BrownFlannelTavernStore.Services.Notifications.Emails;
using FluentAssertions;

namespace BrownFlannelTavernStore.Tests.Services.Notifications.Emails;

public class AdminNewOrderEmailTests
{
    private static Order ShippedOrder() => new()
    {
        Id = 101,
        CustomerEmail = "customer@example.com",
        CustomerName = "Jane Doe",
        Phone = "555-1234",
        FulfillmentMethod = FulfillmentMethod.Shipped,
        ShippingAddress = "123 Main St",
        City = "Westland",
        State = "MI",
        ZipCode = "48186",
        TotalAmount = 49.98m,
        Lines = [
            new OrderLine { ProductName = "Classic Logo Tee", Size = "M", Color = "Black", Quantity = 2, UnitPrice = 24.99m }
        ]
    };

    private static Order PickupOrder() => new()
    {
        Id = 102,
        CustomerEmail = "pickup@example.com",
        CustomerName = "John Smith",
        FulfillmentMethod = FulfillmentMethod.Pickup,
        TotalAmount = 19.99m,
        Lines = [
            new OrderLine { ProductName = "Tavern Baseball Cap", Size = "One Size", Color = "Brown", Quantity = 1, UnitPrice = 19.99m }
        ]
    };

    [Fact]
    public void Build_SetsCorrectMetadata()
    {
        var email = AdminNewOrderEmail.Build(ShippedOrder(), "owner@brownflanneltavern.com");

        email.To.Should().Be("owner@brownflanneltavern.com");
        email.Subject.Should().Be("[BFT Admin] New Order #101 - $49.98");
        email.EmailType.Should().Be(EmailType.AdminAlert);
        email.OrderId.Should().Be(101);
    }

    [Fact]
    public void Build_IncludesCustomerContactInfo()
    {
        var email = AdminNewOrderEmail.Build(ShippedOrder(), "owner@example.com");

        email.HtmlBody.Should().Contain("Jane Doe");
        email.HtmlBody.Should().Contain("customer@example.com");
        email.HtmlBody.Should().Contain("555-1234");
    }

    [Fact]
    public void Build_PhoneOmitted_ShowsNotProvided()
    {
        var order = PickupOrder();
        order.Phone = null;

        var email = AdminNewOrderEmail.Build(order, "owner@example.com");

        email.HtmlBody.Should().Contain("(not provided)");
    }

    [Fact]
    public void Build_ShippedOrder_IncludesShippingAddress()
    {
        var email = AdminNewOrderEmail.Build(ShippedOrder(), "owner@example.com");

        email.HtmlBody.Should().Contain("Shipping");
        email.HtmlBody.Should().Contain("123 Main St");
        email.HtmlBody.Should().Contain("48186");
    }

    [Fact]
    public void Build_PickupOrder_IndicatesPickupAndOmitsAddress()
    {
        var email = AdminNewOrderEmail.Build(PickupOrder(), "owner@example.com");

        email.HtmlBody.Should().Contain("Pickup at store");
        email.HtmlBody.Should().NotContain("Ship to");
    }

    [Fact]
    public void Build_IncludesAdminDashboardLink()
    {
        var email = AdminNewOrderEmail.Build(ShippedOrder(), "owner@example.com");

        email.HtmlBody.Should().Contain("/Admin/Orders/Details/101");
    }

    [Fact]
    public void Build_TextBodyContainsAllKeyInfo()
    {
        var email = AdminNewOrderEmail.Build(ShippedOrder(), "owner@example.com");

        email.TextBody.Should().NotBeNull();
        email.TextBody.Should().Contain("Jane Doe");
        email.TextBody.Should().Contain("customer@example.com");
        email.TextBody.Should().Contain("Classic Logo Tee");
        email.TextBody.Should().Contain("$49.98");
    }
}
