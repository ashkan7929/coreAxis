using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Events;

/// <summary>
/// Domain event raised when an order's price is locked.
/// </summary>
public class OrderPriceLockedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string AssetCode { get; }
    public decimal Quantity { get; }
    public decimal LockedPrice { get; }
    public DateTime LockedAt { get; }
    public DateTime ExpiresAt { get; }
    public string TenantId { get; }

    public OrderPriceLockedEvent(Guid orderId, string assetCode, decimal quantity, decimal lockedPrice, 
        DateTime lockedAt, DateTime expiresAt, string tenantId)
    {
        OrderId = orderId;
        AssetCode = assetCode;
        Quantity = quantity;
        LockedPrice = lockedPrice;
        LockedAt = lockedAt;
        ExpiresAt = expiresAt;
        TenantId = tenantId;
    }
}