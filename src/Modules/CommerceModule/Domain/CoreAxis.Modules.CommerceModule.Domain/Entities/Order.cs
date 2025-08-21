using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a customer order in the commerce system.
/// </summary>
public class Order : EntityBase
{
    /// <summary>
    /// Gets or sets the order number (human-readable identifier).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer ID who placed the order.
    /// </summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the current status of the order.
    /// </summary>
    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>
    /// Gets or sets the priority of the order.
    /// </summary>
    public OrderPriority Priority { get; set; } = OrderPriority.Normal;

    /// <summary>
    /// Gets or sets the subtotal amount (before taxes and discounts).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Gets or sets the tax amount.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the discount amount.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the shipping cost.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; set; } = 0;

    /// <summary>
    /// Gets or sets the total amount of the order.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., USD, EUR).
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the date when the order was placed.
    /// </summary>
    [Required]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the expected delivery date.
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Gets or sets the actual delivery date.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets the date when the order was cancelled.
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Gets or sets the reason for cancellation.
    /// </summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Gets or sets the shipping address as JSON.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// Gets or sets the billing address as JSON.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? BillingAddress { get; set; }

    /// <summary>
    /// Gets or sets additional order metadata as JSON.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for tracking related operations.
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets special instructions for the order.
    /// </summary>
    [MaxLength(1000)]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Gets or sets the collection of order items.
    /// </summary>
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    /// <summary>
    /// Gets or sets the collection of payments for this order.
    /// </summary>
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>
    /// Gets or sets the collection of inventory reservations for this order.
    /// </summary>
    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    /// <summary>
    /// Calculates the total paid amount for this order.
    /// </summary>
    public decimal GetTotalPaidAmount()
    {
        return Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .Sum(p => p.Amount - p.RefundedAmount);
    }

    /// <summary>
    /// Calculates the remaining amount to be paid.
    /// </summary>
    public decimal GetRemainingAmount()
    {
        return Math.Max(0, TotalAmount - GetTotalPaidAmount());
    }

    /// <summary>
    /// Checks if the order is fully paid.
    /// </summary>
    public bool IsFullyPaid()
    {
        return GetRemainingAmount() <= 0;
    }

    /// <summary>
    /// Checks if the order can be cancelled.
    /// </summary>
    public bool CanBeCancelled()
    {
        return Status == OrderStatus.Pending || Status == OrderStatus.Confirmed;
    }

    /// <summary>
    /// Marks the order as confirmed.
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be confirmed");

        Status = OrderStatus.Confirmed;
    }

    /// <summary>
    /// Cancels the order with a reason.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (!CanBeCancelled())
            throw new InvalidOperationException("Order cannot be cancelled in its current state");

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
    }

    /// <summary>
    /// Marks the order as shipped.
    /// </summary>
    public void MarkAsShipped()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed orders can be shipped");

        Status = OrderStatus.Shipped;
    }

    /// <summary>
    /// Marks the order as delivered.
    /// </summary>
    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Only shipped orders can be delivered");

        Status = OrderStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents an item within an order.
/// </summary>
public class OrderItem : EntityBase
{
    /// <summary>
    /// Gets or sets the order ID this item belongs to.
    /// </summary>
    [Required]
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order this item belongs to.
    /// </summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product SKU.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ProductSku { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product name at the time of order.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity ordered.
    /// </summary>
    [Required]
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price at the time of order.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the total price for this line item.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Gets or sets the discount amount for this line item.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets additional item metadata as JSON.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Calculates the final price after discount.
    /// </summary>
    public decimal GetFinalPrice()
    {
        return Math.Max(0, TotalPrice - DiscountAmount);
    }
}