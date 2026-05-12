using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services.Notifications;
using BrownFlannelTavernStore.Services.Notifications.Emails;
using BrownFlannelTavernStore.Tests.TestHelpers;
using FluentAssertions;

namespace BrownFlannelTavernStore.Tests.Services.Notifications.Emails;

public class OrderStatusChangeEmailTests
{
    private static Order OrderAt(OrderStatus status) => new()
    {
        Id = 50,
        CustomerEmail = "customer@example.com",
        CustomerName = "Jane Doe",
        Status = status,
        TotalAmount = 24.99m,
        Lines = [
            new OrderLine { ProductName = "Sticker", Quantity = 1, UnitPrice = 24.99m }
        ]
    };

    [Fact]
    public void Build_SetsCorrectMetadata()
    {
        var email = OrderStatusChangeEmail.Build(OrderAt(OrderStatus.Processing), OrderStatus.Paid, TestBusiness.Default());

        email.To.Should().Be("customer@example.com");
        email.EmailType.Should().Be(EmailType.StatusChange);
        email.OrderId.Should().Be(50);
    }

    [Theory]
    [InlineData(OrderStatus.Processing, "being prepared")]
    [InlineData(OrderStatus.Shipped, "has shipped")]
    [InlineData(OrderStatus.Delivered, "has been delivered")]
    [InlineData(OrderStatus.Cancelled, "has been cancelled")]
    public void Build_KnownStatus_UsesAppropriateHeading(OrderStatus status, string expectedPhrase)
    {
        var email = OrderStatusChangeEmail.Build(OrderAt(status), OrderStatus.Paid, TestBusiness.Default());

        email.HtmlBody.Should().Contain(expectedPhrase);
        email.Subject.Should().Contain(expectedPhrase);
    }

    [Fact]
    public void Build_CancelledStatus_MentionsRefundContact()
    {
        var email = OrderStatusChangeEmail.Build(OrderAt(OrderStatus.Cancelled), OrderStatus.Paid, TestBusiness.Default());

        email.HtmlBody.Should().Contain("refund");
    }

    [Fact]
    public void Build_UnknownStatusFallsThroughToGenericCopy()
    {
        var email = OrderStatusChangeEmail.Build(OrderAt(OrderStatus.Pending), OrderStatus.Paid, TestBusiness.Default());

        email.HtmlBody.Should().Contain("Order status updated");
        email.HtmlBody.Should().Contain("Pending");
    }

    [Fact]
    public void Build_IncludesOrderIdAndTotal()
    {
        var email = OrderStatusChangeEmail.Build(OrderAt(OrderStatus.Shipped), OrderStatus.Processing, TestBusiness.Default());

        email.HtmlBody.Should().Contain("#50");
        email.HtmlBody.Should().Contain("$24.99");
    }

    [Fact]
    public void Build_DeliveredEmail_UsesBusinessName()
    {
        var biz = TestBusiness.Default();
        biz.Name = "Sample Store Co.";

        var email = OrderStatusChangeEmail.Build(OrderAt(OrderStatus.Delivered), OrderStatus.Shipped, biz);

        email.HtmlBody.Should().Contain("Sample Store Co.");
    }

    [Fact]
    public void Build_TextBodyContainsKeyInfoInPlainText()
    {
        var email = OrderStatusChangeEmail.Build(OrderAt(OrderStatus.Shipped), OrderStatus.Processing, TestBusiness.Default());

        email.TextBody.Should().NotBeNull();
        email.TextBody.Should().Contain("Jane Doe");
        email.TextBody.Should().Contain("#50");
        email.TextBody.Should().Contain("on its way");
    }

    [Fact]
    public void Build_HtmlEncodesCustomerName()
    {
        var order = OrderAt(OrderStatus.Processing);
        order.CustomerName = "<script>alert('xss')</script>";

        var email = OrderStatusChangeEmail.Build(order, OrderStatus.Paid, TestBusiness.Default());

        email.HtmlBody.Should().NotContain("<script>");
        email.HtmlBody.Should().Contain("&lt;script&gt;");
    }
}
