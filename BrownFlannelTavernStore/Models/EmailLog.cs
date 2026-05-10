using System.ComponentModel.DataAnnotations;

namespace BrownFlannelTavernStore.Models;

public class EmailLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string ToAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Subject { get; set; } = string.Empty;

    public EmailType EmailType { get; set; }

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    public EmailStatus Status { get; set; }

    [StringLength(200)]
    public string? ProviderMessageId { get; set; }

    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    public string? TextBody { get; set; }

    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeliveryUpdatedAt { get; set; }
}

public enum EmailType
{
    OrderConfirmation,
    ShippingNotification,
    StatusChange,
    RefundConfirmation,
    AdminAlert,
    TestEmail
}

public enum EmailStatus
{
    Sent,
    Failed,
    Delivered,
    Bounced,
    Complained
}
