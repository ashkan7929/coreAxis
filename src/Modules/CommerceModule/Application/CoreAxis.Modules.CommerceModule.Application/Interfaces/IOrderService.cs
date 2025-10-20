using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

/// <summary>
/// Interface for order service operations.
/// </summary>
public interface IOrderService
{
    Task<OrderResult> CreateOrderAsync(
        CreateOrderRequest request,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<OrderResult> UpdateOrderStatusAsync(
        Guid orderId,
        OrderStatus status,
        string? reason = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<Order?> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<List<Order>> GetOrdersByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<OrderResult> CancelOrderAsync(
        Guid orderId,
        string reason,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    // --- Added methods to align with API controller usage ---
    Task<List<Order>> GetOrdersAsync(
        Guid? customerId,
        OrderStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Order?> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<Order> CreateOrderAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<Order> UpdateOrderAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<Order?> CancelOrderAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<Order?> ConfirmOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<Order?> FulfillOrderAsync(
        Guid orderId,
        string trackingNumber,
        string shippingCarrier,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for creating an order.
/// </summary>
public class CreateOrderRequest
{
    public Guid CustomerId { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
    public string? ShippingAddress { get; set; }
    public string? BillingAddress { get; set; }
    public string? Notes { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request model for order items.
/// </summary>
public class OrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Result model for order operations.
/// </summary>
public class OrderResult
{
    public bool Success { get; set; }
    public Guid? OrderId { get; set; }
    public OrderStatus? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}