using CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Orders;

public class Order : EntityBase
{
    public string UserId { get; private set; }
    public AssetCode AssetCode { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? PriceLockExpiresAt { get; private set; }
    public decimal? LockedPrice { get; private set; }
    public string? IdempotencyKey { get; private set; }
    
    private readonly List<OrderLine> _orderLines = new();
    public IReadOnlyList<OrderLine> OrderLines => _orderLines.AsReadOnly();

    private Order() { } // For EF Core

    private Order(string userId, AssetCode assetCode, decimal totalAmount, List<OrderLine> orderLines)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        AssetCode = assetCode;
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        
        foreach (var orderLine in orderLines)
        {
            orderLine.SetOrderId(Id);
            _orderLines.Add(orderLine);
        }
    }

    public static Order Create(string userId, AssetCode assetCode, decimal totalAmount, List<OrderLine> orderLines)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
        
        if (totalAmount <= 0)
            throw new ArgumentException("TotalAmount must be greater than zero", nameof(totalAmount));
        
        if (orderLines == null || !orderLines.Any())
            throw new ArgumentException("Order must have at least one order line", nameof(orderLines));

        return new Order(userId, assetCode, totalAmount, orderLines);
    }

    public void UpdateStatus(OrderStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPriceLock(decimal lockedPrice, DateTime expiresAt)
    {
        LockedPrice = lockedPrice;
        PriceLockExpiresAt = expiresAt;
        Status = OrderStatus.PriceLocked;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(idempotencyKey));
            
        IdempotencyKey = idempotencyKey;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Completed || Status == OrderStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel order with status {Status}");
        
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != OrderStatus.PriceLocked)
            throw new InvalidOperationException($"Cannot complete order with status {Status}");
        
        Status = OrderStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
}