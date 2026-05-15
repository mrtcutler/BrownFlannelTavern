using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrownFlannelTavernStore.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    public string StripePaymentIntentId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    public FulfillmentMethod FulfillmentMethod { get; set; } = FulfillmentMethod.Shipped;

    public NotificationPreference NotificationPreference { get; set; } = NotificationPreference.Email;

    [StringLength(500)]
    public string? ShippingAddress { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(100)]
    public string? TaxCalculationId { get; set; }

    [StringLength(100)]
    public string? StripeRefundId { get; set; }

    public DateTime? RefundedAt { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<OrderLine> Lines { get; set; } = [];
}

public class OrderLine
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public int? VariantId { get; set; }

    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public string? Size { get; set; }
    public string? Color { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
}

public enum OrderStatus
{
    Pending,
    Paid,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}

public enum FulfillmentMethod
{
    Shipped,
    Pickup
}

public enum NotificationPreference
{
    Email,
    Sms,
    Both
}
