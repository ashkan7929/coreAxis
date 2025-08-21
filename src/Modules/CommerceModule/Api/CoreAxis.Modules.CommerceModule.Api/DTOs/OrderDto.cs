using CoreAxis.Modules.CommerceModule.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.CommerceModule.Api.DTOs;

/// <summary>
/// DTO for order information
/// </summary>
public class OrderDto
{
    /// <summary>
    /// Gets or sets the order ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the customer ID
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the order status
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the order date
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Gets or sets the shipping address
    /// </summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the billing address
    /// </summary>
    public string BillingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subtotal amount
    /// </summary>
    public decimal SubtotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the discount amount
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the tax amount
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the tracking number
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Gets or sets the shipping carrier
    /// </summary>
    public string? ShippingCarrier { get; set; }

    /// <summary>
    /// Gets or sets the last modified date
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Gets or sets the order items
    /// </summary>
    public List<OrderItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for order item information
/// </summary>
public class OrderItemDto
{
    /// <summary>
    /// Gets or sets the order item ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product ID
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the total price
    /// </summary>
    public decimal TotalPrice { get; set; }
}

/// <summary>
/// DTO for creating a new order
/// </summary>
public class CreateOrderDto
{
    /// <summary>
    /// Gets or sets the customer ID
    /// </summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the shipping address
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the billing address (optional, defaults to shipping address)
    /// </summary>
    [StringLength(500, MinimumLength = 10)]
    public string? BillingAddress { get; set; }

    /// <summary>
    /// Gets or sets the order items
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one item")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for creating a new order item
/// </summary>
public class CreateOrderItemDto
{
    /// <summary>
    /// Gets or sets the product ID
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the quantity
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// DTO for updating an existing order
/// </summary>
public class UpdateOrderDto
{
    /// <summary>
    /// Gets or sets the shipping address
    /// </summary>
    [StringLength(500, MinimumLength = 10)]
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// Gets or sets the billing address
    /// </summary>
    [StringLength(500, MinimumLength = 10)]
    public string? BillingAddress { get; set; }

    /// <summary>
    /// Gets or sets the order status
    /// </summary>
    public OrderStatus? Status { get; set; }
}

/// <summary>
/// DTO for cancelling an order
/// </summary>
public class CancelOrderDto
{
    /// <summary>
    /// Gets or sets the cancellation reason
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for fulfilling an order
/// </summary>
public class FulfillOrderDto
{
    /// <summary>
    /// Gets or sets the tracking number
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 5)]
    public string TrackingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shipping carrier
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string ShippingCarrier { get; set; } = string.Empty;
}