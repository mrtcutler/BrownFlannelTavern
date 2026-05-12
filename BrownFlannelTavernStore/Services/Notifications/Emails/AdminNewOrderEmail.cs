using System.Net;
using System.Text;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Models.Settings;

namespace BrownFlannelTavernStore.Services.Notifications.Emails;

public static class AdminNewOrderEmail
{
    public static EmailMessage Build(Order order, string adminEmail, BusinessSettings business)
    {
        var prefix = string.IsNullOrWhiteSpace(business.ShortName) ? business.Name : business.ShortName;
        var subject = $"[{prefix} Admin] New Order #{order.Id} - ${order.TotalAmount:F2}";
        return new EmailMessage(
            To: adminEmail,
            Subject: subject,
            HtmlBody: BuildHtmlBody(order, business),
            EmailType: EmailType.AdminAlert,
            TextBody: BuildTextBody(order, business),
            OrderId: order.Id);
    }

    private static string BuildHtmlBody(Order order, BusinessSettings business)
    {
        var customerName = WebUtility.HtmlEncode(order.CustomerName);
        var customerEmail = WebUtility.HtmlEncode(order.CustomerEmail);
        var phone = string.IsNullOrWhiteSpace(order.Phone) ? "(not provided)" : WebUtility.HtmlEncode(order.Phone);

        var lines = string.Concat(order.Lines.Select(line => $"""
                <tr>
                    <td>{WebUtility.HtmlEncode(line.ProductName)}</td>
                    <td>{FormatVariant(line.Size, line.Color)}</td>
                    <td>{line.Quantity}</td>
                    <td>${(line.UnitPrice * line.Quantity):F2}</td>
                </tr>
        """));

        var fulfillment = order.FulfillmentMethod == FulfillmentMethod.Pickup
            ? "<p><strong>Fulfillment:</strong> Pickup at store</p>"
            : $"""
                <p><strong>Fulfillment:</strong> Shipping</p>
                <p><strong>Ship to:</strong><br>
                {WebUtility.HtmlEncode(order.ShippingAddress ?? "")}<br>
                {WebUtility.HtmlEncode(order.City ?? "")}, {WebUtility.HtmlEncode(order.State ?? "")} {WebUtility.HtmlEncode(order.ZipCode ?? "")}</p>
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
                .alert { background: #fef3c7; border-left: 4px solid #d4a96a; padding: 10px 15px; margin-bottom: 20px; }
            </style>
        </head>
        <body>
            <div class="alert">
                <strong>New order received</strong> — Order #{{order.Id}} for ${{order.TotalAmount:F2}}
            </div>

            <h2>Customer</h2>
            <p>
                <strong>Name:</strong> {{customerName}}<br>
                <strong>Email:</strong> {{customerEmail}}<br>
                <strong>Phone:</strong> {{phone}}
            </p>

        {{fulfillment}}

            <h3>Order Items</h3>
            <table>
                <thead>
                    <tr><th>Item</th><th>Size / Color</th><th>Qty</th><th>Price</th></tr>
                </thead>
                <tbody>
        {{lines}}
                    <tr class="total"><td colspan="3">Total</td><td>${{order.TotalAmount:F2}}</td></tr>
                </tbody>
            </table>
        </body>
        </html>
        """;
    }

    private static string BuildTextBody(Order order, BusinessSettings business)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"NEW ORDER RECEIVED - Order #{order.Id} - ${order.TotalAmount:F2}");
        sb.AppendLine();
        sb.AppendLine("CUSTOMER");
        sb.AppendLine($"  Name: {order.CustomerName}");
        sb.AppendLine($"  Email: {order.CustomerEmail}");
        sb.AppendLine($"  Phone: {(string.IsNullOrWhiteSpace(order.Phone) ? "(not provided)" : order.Phone)}");
        sb.AppendLine();
        if (order.FulfillmentMethod == FulfillmentMethod.Pickup)
        {
            sb.AppendLine("FULFILLMENT: Pickup at store");
        }
        else
        {
            sb.AppendLine("FULFILLMENT: Shipping");
            sb.AppendLine($"  Ship to: {order.ShippingAddress}");
            sb.AppendLine($"           {order.City}, {order.State} {order.ZipCode}");
        }
        sb.AppendLine();
        sb.AppendLine("ITEMS");
        foreach (var line in order.Lines)
        {
            var variant = FormatVariant(line.Size, line.Color);
            var variantPart = string.IsNullOrEmpty(variant) ? "" : $" ({variant})";
            sb.AppendLine($"  {line.ProductName}{variantPart} - Qty {line.Quantity} - ${(line.UnitPrice * line.Quantity):F2}");
        }
        sb.AppendLine();
        sb.AppendLine($"Total: ${order.TotalAmount:F2}");
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
