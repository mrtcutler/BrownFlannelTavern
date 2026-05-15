using System.Net;
using System.Text;
using BrownFlannelTavernStore.Models;
using BrownFlannelTavernStore.Models.Settings;

namespace BrownFlannelTavernStore.Services.Notifications.Emails;

public static class RefundConfirmationEmail
{
    public static EmailMessage Build(Order order, BusinessSettings business)
    {
        var subject = $"{business.Name} - Refund Issued for Order #{order.Id}";
        return new EmailMessage(
            To: order.CustomerEmail,
            Subject: subject,
            HtmlBody: BuildHtmlBody(order, business),
            EmailType: EmailType.RefundConfirmation,
            TextBody: BuildTextBody(order, business),
            OrderId: order.Id);
    }

    private static string BuildHtmlBody(Order order, BusinessSettings business)
    {
        var businessName = WebUtility.HtmlEncode(business.Name);
        var name = WebUtility.HtmlEncode(order.CustomerName);

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <style>
                body { font-family: Arial, sans-serif; color: #2c2c2c; max-width: 600px; margin: 0 auto; padding: 20px; }
                h1, h2 { color: #5C3A1E; }
                .refund-card { background: #f5ede0; border-left: 4px solid #5C3A1E; padding: 15px 20px; margin-bottom: 20px; }
                .footer { color: #777; font-size: 12px; margin-top: 30px; border-top: 1px solid #ddd; padding-top: 15px; }
            </style>
        </head>
        <body>
            <h1>{{businessName}}</h1>
            <p>Hi {{name}},</p>

            <div class="refund-card">
                <h2>Your refund has been issued</h2>
                <p>We've refunded <strong>${{order.TotalAmount:F2}}</strong> for order <strong>#{{order.Id}}</strong>.</p>
                <p>Depending on your bank, it can take 5–10 business days for the funds to appear on your statement.</p>
            </div>

            <p>If you have any questions about this refund, just reply to this email.</p>

            <p class="footer">Thanks for shopping with {{businessName}}.</p>
        </body>
        </html>
        """;
    }

    private static string BuildTextBody(Order order, BusinessSettings business)
    {
        var sb = new StringBuilder();
        sb.AppendLine(business.Name);
        sb.AppendLine();
        sb.AppendLine($"Hi {order.CustomerName},");
        sb.AppendLine();
        sb.AppendLine("YOUR REFUND HAS BEEN ISSUED");
        sb.AppendLine($"We've refunded ${order.TotalAmount:F2} for order #{order.Id}.");
        sb.AppendLine("Depending on your bank, it can take 5–10 business days for the funds to appear on your statement.");
        sb.AppendLine();
        sb.AppendLine("If you have any questions about this refund, just reply to this email.");
        sb.AppendLine();
        sb.AppendLine($"Thanks for shopping with {business.Name}.");
        return sb.ToString();
    }
}
