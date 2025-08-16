using CoreAxis.SharedKernel;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using CoreAxis.Modules.ProductOrderModule.Domain.Events;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Entities;

/// <summary>
/// Represents an order in the product order system.
/// </summary>
public class Order : EntityBase
{
    private readonly List<OrderLine> _orderLines = new();

    public Guid UserId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public OrderType OrderType { get; private set; }
    public OrderStatus Status { get; private set; }
    public AssetCode AssetCode { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public Money? LockedPrice { get; private set; }
    public DateTime? PriceLockedAt { get; private set; }
    public DateTime? PriceExpiresAt { get; private set; }
    public Money? TotalAmount { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string JsonSnapshot { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    /// <summary>
    /// Gets the order lines associated with this order.
    /// </summary>
    public IReadOnlyCollection<OrderLine> OrderLines => _orderLines.AsReadOnly();

    // Private constructor for EF Core
    private Order() { }

    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="userId">The user placing the order</param>
    /// <param name="orderType">The type of order (buy/sell)</param>
    /// <param name="assetCode">The asset being traded</param>
    /// <param name="quantity">The quantity to trade</param>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="idempotencyKey">Optional idempotency key for duplicate prevention</param>
    /// <param name="notes">Optional notes for the order</param>
    /// <returns>A new Order instance</returns>
    public static Order Create(
        Guid userId,
        OrderType orderType,
        AssetCode assetCode,
        decimal quantity,
        string tenantId,
        string? idempotencyKey = null,
        string? notes = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

        var order = new Order
        {
            UserId = userId,
            OrderNumber = GenerateOrderNumber(),
            OrderType = orderType,
            Status = OrderStatus.Pending,
            AssetCode = assetCode,
            Quantity = quantity,
            TenantId = tenantId,
            IdempotencyKey = idempotencyKey,
            Notes = notes
        };

        order.UpdateJsonSnapshot();
        order.AddDomainEvent(new OrderCreatedEvent(order.Id, userId, assetCode.Value, quantity, tenantId));

        return order;
    }

    /// <summary>
    /// Locks the price for this order.
    /// </summary>
    /// <param name="lockedPrice">The locked price</param>
    /// <param name="expiryDuration">How long the price lock is valid</param>
    public void LockPrice(Money lockedPrice, TimeSpan expiryDuration)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot lock price for order in {Status} status.");

        if (lockedPrice == null || !lockedPrice.IsPositive)
            throw new ArgumentException("Locked price must be positive.", nameof(lockedPrice));

        LockedPrice = lockedPrice;
        PriceLockedAt = DateTime.UtcNow;
        PriceExpiresAt = PriceLockedAt.Value.Add(expiryDuration);
        Status = OrderStatus.PriceLocked;
        TotalAmount = lockedPrice * Quantity;

        UpdateJsonSnapshot();
        AddDomainEvent(new OrderPriceLockedEvent(Id, AssetCode.Value, Quantity, lockedPrice.Amount, 
            PriceLockedAt.Value, PriceExpiresAt.Value, TenantId));
    }

    /// <summary>
    /// Confirms the order.
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.PriceLocked)
            throw new InvalidOperationException($"Cannot confirm order in {Status} status.");

        if (PriceExpiresAt.HasValue && DateTime.UtcNow > PriceExpiresAt.Value)
            throw new InvalidOperationException("Cannot confirm order with expired price lock.");

        Status = OrderStatus.Confirmed;
        UpdateJsonSnapshot();
        AddDomainEvent(new OrderConfirmedEvent(Id, UserId, TenantId));
    }

    /// <summary>
    /// Completes the order.
    /// </summary>
    public void Complete()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot complete order in {Status} status.");

        Status = OrderStatus.Completed;
        UpdateJsonSnapshot();
        AddDomainEvent(new OrderCompletedEvent(Id, UserId, TotalAmount?.Amount ?? 0, TenantId));
    }

    /// <summary>
    /// Cancels the order.
    /// </summary>
    /// <param name="reason">The reason for cancellation</param>
    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed order.");

        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled.");

        Status = OrderStatus.Cancelled;
        Notes = string.IsNullOrEmpty(Notes) ? reason : $"{Notes}; Cancelled: {reason}";
        UpdateJsonSnapshot();
        AddDomainEvent(new OrderCancelledEvent(Id, UserId, reason, TenantId));
    }

    /// <summary>
    /// Expires the order due to price lock expiry.
    /// </summary>
    public void Expire()
    {
        if (Status != OrderStatus.PriceLocked)
            throw new InvalidOperationException($"Cannot expire order in {Status} status.");

        Status = OrderStatus.Expired;
        UpdateJsonSnapshot();
        AddDomainEvent(new OrderExpiredEvent(Id, UserId, TenantId));
    }

    /// <summary>
    /// Adds an order line to this order.
    /// </summary>
    /// <param name="orderLine">The order line to add</param>
    public void AddOrderLine(OrderLine orderLine)
    {
        if (orderLine == null)
            throw new ArgumentNullException(nameof(orderLine));

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot add order lines to order in {Status} status.");

        _orderLines.Add(orderLine);
        UpdateJsonSnapshot();
    }

    /// <summary>
    /// Checks if the price lock has expired.
    /// </summary>
    /// <returns>True if the price lock has expired, false otherwise</returns>
    public bool IsPriceLockExpired()
    {
        return PriceExpiresAt.HasValue && DateTime.UtcNow > PriceExpiresAt.Value;
    }

    /// <summary>
    /// Updates the JSON snapshot of the order.
    /// </summary>
    private void UpdateJsonSnapshot()
    {
        var snapshot = new
        {
            Id,
            UserId,
            OrderNumber,
            OrderType = OrderType.ToString(),
            Status = Status.ToString(),
            AssetCode = AssetCode?.Value,
            Quantity,
            LockedPrice = LockedPrice?.Amount,
            LockedPriceCurrency = LockedPrice?.Currency,
            PriceLockedAt,
            PriceExpiresAt,
            TotalAmount = TotalAmount?.Amount,
            TotalAmountCurrency = TotalAmount?.Currency,
            IdempotencyKey,
            TenantId,
            Notes,
            CreatedOn,
            LastModifiedOn,
            OrderLines = _orderLines.Select(ol => new
            {
                ol.Id,
                AssetCode = ol.AssetCode.Value,
                ol.Quantity,
                UnitPriceAmount = ol.UnitPrice?.Amount,
                UnitPriceCurrency = ol.UnitPrice?.Currency,
                LineTotalAmount = ol.LineTotal?.Amount,
                LineTotalCurrency = ol.LineTotal?.Currency
            }).ToList()
        };

        JsonSnapshot = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Generates a unique order number.
    /// </summary>
    /// <returns>A unique order number</returns>
    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}