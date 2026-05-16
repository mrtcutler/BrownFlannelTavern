using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services.Notifications;
using BrownFlannelTavernStore.Services.Notifications.Emails;
using BrownFlannelTavernStore.Tests.TestHelpers;
using FluentAssertions;

namespace BrownFlannelTavernStore.Tests.Services.Notifications.Emails;

public class OrderConfirmationEmailTests
{
    private const string TestViewUrl = "https://example.com/Orders/View?token=test";

    private static Order ShippedOrder() => new()
    {
        Id = 42,
        CustomerEmail = "customer@example.com",
        CustomerName = "Jane Doe",
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
        Id = 43,
        CustomerEmail = "pickup@example.com",
        CustomerName = "John Smith",
        FulfillmentMethod = FulfillmentMethod.Pickup,
        TotalAmount = 24.99m,
        Lines = [
            new OrderLine { ProductName = "Tavern Baseball Cap", Size = "One Size", Color = "Brown", Quantity = 1, UnitPrice = 19.99m },
            new OrderLine { ProductName = "Sticker Pack", Quantity = 1, UnitPrice = 5.00m }
        ]
    };

    [Fact]
    public void Build_SetsCorrectMetadata()
    {
        var email = OrderConfirmationEmail.Build(ShippedOrder(), TestBusiness.Default(), TestViewUrl);

        email.To.Should().Be("customer@example.com");
        email.Subject.Should().Be("Brown Flannel Tavern - Order #42 Confirmation");
        email.EmailType.Should().Be(EmailType.OrderConfirmation);
        email.OrderId.Should().Be(42);
    }

    [Fact]
    public void Build_HtmlBodyContainsCustomerNameOrderIdAndTotal()
    {
        var email = OrderConfirmationEmail.Build(ShippedOrder(), TestBusiness.Default(), TestViewUrl);

        email.HtmlBody.Should().Contain("Jane Doe");
        email.HtmlBody.Should().Contain("#42");
        email.HtmlBody.Should().Contain("$49.98");
    }

    [Fact]
    public void Build_HtmlBodyListsAllLineItems()
    {
        var email = OrderConfirmationEmail.Build(PickupOrder(), TestBusiness.Default(), TestViewUrl);

        email.HtmlBody.Should().Contain("Tavern Baseball Cap");
        email.HtmlBody.Should().Contain("Sticker Pack");
    }

    [Fact]
    public void Build_ShippedOrder_IncludesShippingAddress()
    {
        var email = OrderConfirmationEmail.Build(ShippedOrder(), TestBusiness.Default(), TestViewUrl);

        email.HtmlBody.Should().Contain("123 Main St");
        email.HtmlBody.Should().Contain("Westland");
        email.HtmlBody.Should().Contain("MI");
        email.HtmlBody.Should().Contain("48186");
        email.HtmlBody.Should().Contain("tracking information");
    }

    [Fact]
    public void Build_PickupOrder_IncludesPickupAddressFromBusinessSettings()
    {
        var email = OrderConfirmationEmail.Build(PickupOrder(), TestBusiness.Default(), TestViewUrl);

        email.HtmlBody.Should().Contain("175 S Venoy Rd");
        email.HtmlBody.Should().Contain("ready for pickup");
        email.HtmlBody.Should().NotContain("tracking");
    }

    [Fact]
    public void Build_PickupOrder_UsesBusinessNameInHeaderAndPickupLocation()
    {
        var biz = TestBusiness.Default();
        biz.Name = "Sample Store Co.";
        biz.Pickup.LocationName = "Sample Store Co. HQ";
        biz.Pickup.AddressLine1 = "999 Other St";
        biz.Pickup.City = "Anytown";
        biz.Pickup.State = "CA";
        biz.Pickup.PostalCode = "90210";

        var email = OrderConfirmationEmail.Build(PickupOrder(), biz, TestViewUrl);

        email.Subject.Should().Be("Sample Store Co. - Order #43 Confirmation");
        email.HtmlBody.Should().Contain("Sample Store Co.");
        email.HtmlBody.Should().Contain("Sample Store Co. HQ");
        email.HtmlBody.Should().Contain("999 Other St");
        email.HtmlBody.Should().NotContain("175 S Venoy Rd");
    }

    [Fact]
    public void Build_PickupOrder_IncludesInstructionsWhenSet()
    {
        var biz = TestBusiness.Default();
        biz.Pickup.Instructions = "Ask for your order at the bar.";

        var email = OrderConfirmationEmail.Build(PickupOrder(), biz, TestViewUrl);

        email.HtmlBody.Should().Contain("Ask for your order at the bar.");
        email.TextBody.Should().Contain("Ask for your order at the bar.");
    }

    [Fact]
    public void Build_HtmlBody_IncludesSubtotalTaxAndTotalRows()
    {
        var order = ShippedOrder();
        order.Subtotal = 45.00m;
        order.TaxAmount = 2.70m;
        order.TotalAmount = 47.70m;

        var email = OrderConfirmationEmail.Build(order, TestBusiness.Default(), TestViewUrl);

        email.HtmlBody.Should().Contain("Subtotal");
        email.HtmlBody.Should().Contain("$45.00");
        email.HtmlBody.Should().Contain("Tax");
        email.HtmlBody.Should().Contain("$2.70");
        email.HtmlBody.Should().Contain("$47.70");
    }

    [Fact]
    public void Build_TextBody_IncludesSubtotalTaxAndTotalLines()
    {
        var order = ShippedOrder();
        order.Subtotal = 45.00m;
        order.TaxAmount = 2.70m;
        order.TotalAmount = 47.70m;

        var email = OrderConfirmationEmail.Build(order, TestBusiness.Default(), TestViewUrl);

        email.TextBody.Should().Contain("Subtotal: $45.00");
        email.TextBody.Should().Contain("Tax:      $2.70");
        email.TextBody.Should().Contain("Total:    $47.70");
    }

    [Fact]
    public void Build_HtmlBodyIncludesMagicLink()
    {
        var url = "https://bft.example.com/Orders/View?token=abc123";
        var email = OrderConfirmationEmail.Build(ShippedOrder(), TestBusiness.Default(), url);

        email.HtmlBody.Should().Contain($"href=\"{url}\"");
        email.TextBody.Should().Contain(url);
    }

    [Fact]
    public void Build_HtmlEncodesUnsafeCustomerName()
    {
        var order = ShippedOrder();
        order.CustomerName = "<script>alert('xss')</script>";

        var email = OrderConfirmationEmail.Build(order, TestBusiness.Default(), TestViewUrl);

        email.HtmlBody.Should().NotContain("<script>");
        email.HtmlBody.Should().Contain("&lt;script&gt;");
    }
}
