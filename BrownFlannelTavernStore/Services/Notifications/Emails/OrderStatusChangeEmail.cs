using System.Net;
using System.Text;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Services.Notifications.Emails;

public static class OrderStatusChangeEmail
{
    public static EmailMessage Build(Order order, OrderStatus previousStatus)
    {
        var (heading, _) = StatusCopy(order.Status);
        var subject = $"Brown Flannel Tavern - Order #{order.Id} - {heading}";
        return new EmailMessage(
            To: order.CustomerEmail,
            Subject: subject,
            HtmlBody: BuildHtmlBody(order, previousStatus),
            EmailType: EmailType.StatusChange,
            TextBody: BuildTextBody(order, previousStatus),
            OrderId: order.Id);
    }

    private static (string Heading, string Body) StatusCopy(OrderStatus status) => status switch
    {
        OrderStatus.Processing => ("Your order is being prepared",
            "We're getting your order ready. We'll send you another update when it's on its way."),
        OrderStatus.Shipped => ("Your order has shipped",
            "Your order is on its way. We'll follow up if tracking information becomes available."),
        OrderStatus.Delivered => ("Your order has been delivered",
            "Your order has been delivered. We hope you enjoy it — thanks for supporting the Brown Flannel Tavern!"),
        OrderStatus.Cancelled => ("Your order has been cancelled",
            "Your order has been cancelled. If you have any questions about a refund or what happens next, please reply to this email."),
        _ => ("Order status updated",
            $"Your order status has been updated to {status}.")
    };

    private static string BuildHtmlBody(Order order, OrderStatus previousStatus)
    {
        var name = WebUtility.HtmlEncode(order.CustomerName);
        var (heading, body) = StatusCopy(order.Status);
        var safeHeading = WebUtility.HtmlEncode(heading);
        var safeBody = WebUtility.HtmlEncode(body);

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <style>
                body { font-family: Arial, sans-serif; color: #2c2c2c; max-width: 600px; margin: 0 auto; padding: 20px; }
                h1, h2 { color: #5C3A1E; }
                .status-card { background: #f5ede0; border-left: 4px solid #5C3A1E; padding: 15px 20px; margin-bottom: 20px; }
                .footer { color: #777; font-size: 12px; margin-top: 30px; border-top: 1px solid #ddd; padding-top: 15px; }
            </style>
        </head>
        <body>
            <h1>Brown Flannel Tavern</h1>
            <p>Hi {{name}},</p>

            <div class="status-card">
                <h2>{{safeHeading}}</h2>
                <p>{{safeBody}}</p>
            </div>

            <p><strong>Order #{{order.Id}}</strong><br>
            Total: ${{order.TotalAmount:F2}}</p>

            <p class="footer">Questions? Reply to this email or contact us at the tavern.</p>
        </body>
        </html>
        """;
    }

    private static string BuildTextBody(Order order, OrderStatus previousStatus)
    {
        var (heading, body) = StatusCopy(order.Status);
        var sb = new StringBuilder();
        sb.AppendLine("Brown Flannel Tavern");
        sb.AppendLine();
        sb.AppendLine($"Hi {order.CustomerName},");
        sb.AppendLine();
        sb.AppendLine(heading.ToUpper());
        sb.AppendLine(body);
        sb.AppendLine();
        sb.AppendLine($"Order #{order.Id}");
        sb.AppendLine($"Total: ${order.TotalAmount:F2}");
        sb.AppendLine();
        sb.AppendLine("Questions? Reply to this email or contact us at the tavern.");
        return sb.ToString();
    }
}
