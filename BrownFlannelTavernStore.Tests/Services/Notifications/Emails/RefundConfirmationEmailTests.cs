using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Services.Notifications;
using BrownFlannelTavernStore.Services.Notifications.Emails;
using BrownFlannelTavernStore.Tests.TestHelpers;
using FluentAssertions;

namespace BrownFlannelTavernStore.Tests.Services.Notifications.Emails;

public class RefundConfirmationEmailTests
{
    private static Order RefundedOrder() => new()
    {
        Id = 77,
        CustomerEmail = "customer@example.com",
        CustomerName = "Jane Doe",
        Status = OrderStatus.Refunded,
        TotalAmount = 47.70m,
        RefundedAt = new DateTime(2026, 5, 14, 12, 0, 0, DateTimeKind.Utc),
        StripeRefundId = "re_test_abc123",
    };

    [Fact]
    public void Build_SetsCorrectMetadata()
    {
        var email = RefundConfirmationEmail.Build(RefundedOrder(), TestBusiness.Default());

        email.To.Should().Be("customer@example.com");
        email.Subject.Should().Be("Brown Flannel Tavern - Refund Issued for Order #77");
        email.EmailType.Should().Be(EmailType.RefundConfirmation);
        email.OrderId.Should().Be(77);
    }

    [Fact]
    public void Build_HtmlBody_IncludesCustomerNameOrderIdAndRefundAmount()
    {
        var email = RefundConfirmationEmail.Build(RefundedOrder(), TestBusiness.Default());

        email.HtmlBody.Should().Contain("Jane Doe");
        email.HtmlBody.Should().Contain("#77");
        email.HtmlBody.Should().Contain("$47.70");
    }

    [Fact]
    public void Build_TextBody_IncludesRefundAmountAndOrderId()
    {
        var email = RefundConfirmationEmail.Build(RefundedOrder(), TestBusiness.Default());

        email.TextBody.Should().Contain("$47.70");
        email.TextBody.Should().Contain("#77");
        email.TextBody.Should().Contain("YOUR REFUND HAS BEEN ISSUED");
    }

    [Fact]
    public void Build_UsesBusinessNameInHeaderAndSubject()
    {
        var biz = TestBusiness.Default();
        biz.Name = "Sample Store Co.";

        var email = RefundConfirmationEmail.Build(RefundedOrder(), biz);

        email.Subject.Should().Be("Sample Store Co. - Refund Issued for Order #77");
        email.HtmlBody.Should().Contain("Sample Store Co.");
    }

    [Fact]
    public void Build_HtmlEncodesUnsafeCustomerName()
    {
        var order = RefundedOrder();
        order.CustomerName = "<script>alert('xss')</script>";

        var email = RefundConfirmationEmail.Build(order, TestBusiness.Default());

        email.HtmlBody.Should().NotContain("<script>");
        email.HtmlBody.Should().Contain("&lt;script&gt;");
    }
}
