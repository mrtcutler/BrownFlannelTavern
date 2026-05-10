using System.Net;
using System.Text;
using BrownFlannelTavernStore.Models;

namespace BrownFlannelTavernStore.Services.Notifications.Emails;

public static class OrderConfirmationEmail
{
    public static EmailMessage Build(Order order)
    {
        var subject = $"Brown Flannel Tavern - Order #{order.Id} Confirmation";
        return new EmailMessage(
            To: order.CustomerEmail,
            Subject: subject,
            HtmlBody: BuildHtmlBody(order),
            EmailType: EmailType.OrderConfirmation,
            TextBody: BuildTextBody(order),
            OrderId: order.Id);
    }

    private static string BuildHtmlBody(Order order)
    {
        var name = WebUtility.HtmlEncode(order.CustomerName);
        var lines = string.Concat(order.Lines.Select(line => $"""
                <tr>
                    <td>{WebUtility.HtmlEncode(line.ProductName)}</td>
                    <td>{FormatVariant(line.Size, line.Color)}</td>
                    <td>{line.Quantity}</td>
                    <td>${(line.UnitPrice * line.Quantity):F2}</td>
                </tr>
        """));

        var fulfillmentBlock = order.FulfillmentMethod == FulfillmentMethod.Pickup
            ? """
                <p><strong>Pick up at:</strong><br>
                Brown Flannel Tavern<br>
                175 S Venoy Rd, Westland, MI 48186<br>
                Hours: 11 AM – 2 AM, 7 days a week (Holiday hours may vary)</p>
                <p>You'll be notified when your order is ready for pickup.</p>
            """
            : $"""
                <p><strong>Shipping to:</strong><br>
                {WebUtility.HtmlEncode(order.ShippingAddress ?? "")}<br>
                {WebUtility.HtmlEncode(order.City ?? "")}, {WebUtility.HtmlEncode(order.State ?? "")} {WebUtility.HtmlEncode(order.ZipCode ?? "")}</p>
                <p>You'll receive another email with tracking information when your order ships.</p>
            """;

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <style>
                body { font-family: Arial, sans-serif; color: #2c2c2c; max-width: 600px; margin: 0 auto; padding: 20px; }
                h1, h2, h3 { color: #5C3A1E; }
                table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }
                th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }
                .total td { font-weight: bold; border-top: 2px solid #5C3A1E; border-bottom: none; }
                .footer { color: #777; font-size: 12px; margin-top: 30px; border-top: 1px solid #ddd; padding-top: 15px; }
            </style>
        </head>
        <body>
            <h1>Brown Flannel Tavern</h1>
            <h2>Thanks for your order, {{name}}!</h2>
            <p>Your order <strong>#{{order.Id}}</strong> has been received and paid.</p>

            <h3>Order Summary</h3>
            <table>
                <thead>
                    <tr><th>Item</th><th>Size / Color</th><th>Qty</th><th>Price</th></tr>
                </thead>
                <tbody>
        {{lines}}
                    <tr class="total"><td colspan="3">Total</td><td>${{order.TotalAmount:F2}}</td></tr>
                </tbody>
            </table>

            <h3>Fulfillment</h3>
        {{fulfillmentBlock}}

            <p class="footer">Questions? Reply to this email or contact us at the tavern.</p>
        </body>
        </html>
        """;
    }

    private static string BuildTextBody(Order order)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Brown Flannel Tavern");
        sb.AppendLine("Order Confirmation");
        sb.AppendLine();
        sb.AppendLine($"Hi {order.CustomerName},");
        sb.AppendLine();
        sb.AppendLine($"Thanks for your order! Order #{order.Id} has been received and paid.");
        sb.AppendLine();
        sb.AppendLine("ORDER SUMMARY");
        foreach (var line in order.Lines)
        {
            var variant = FormatVariant(line.Size, line.Color);
            var variantPart = string.IsNullOrEmpty(variant) ? "" : $" ({variant})";
            sb.AppendLine($"  {line.ProductName}{variantPart} - Qty {line.Quantity} - ${(line.UnitPrice * line.Quantity):F2}");
        }
        sb.AppendLine();
        sb.AppendLine($"Total: ${order.TotalAmount:F2}");
        sb.AppendLine();
        sb.AppendLine("FULFILLMENT");
        if (order.FulfillmentMethod == FulfillmentMethod.Pickup)
        {
            sb.AppendLine("Pick up at:");
            sb.AppendLine("Brown Flannel Tavern");
            sb.AppendLine("175 S Venoy Rd, Westland, MI 48186");
            sb.AppendLine("Hours: 11 AM – 2 AM, 7 days a week (Holiday hours may vary)");
            sb.AppendLine();
            sb.AppendLine("You'll be notified when your order is ready for pickup.");
        }
        else
        {
            sb.AppendLine("Shipping to:");
            sb.AppendLine(order.ShippingAddress);
            sb.AppendLine($"{order.City}, {order.State} {order.ZipCode}");
            sb.AppendLine();
            sb.AppendLine("You'll receive another email with tracking information when your order ships.");
        }
        sb.AppendLine();
        sb.AppendLine("Questions? Reply to this email or contact us at the tavern.");
        return sb.ToString();
    }

    private static string FormatVariant(string? size, string? color)
    {
        if (string.IsNullOrEmpty(size) && string.IsNullOrEmpty(color)) return "";
        if (string.IsNullOrEmpty(color)) return WebUtility.HtmlEncode(size!);
        if (string.IsNullOrEmpty(size)) return WebUtility.HtmlEncode(color);
        return $"{WebUtility.HtmlEncode(size)} / {WebUtility.HtmlEncode(color)}";
    }
}
