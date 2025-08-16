using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Events;

/// <summary>
/// Domain event raised when a new order is created.
/// </summary>
public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public string AssetCode { get; }
    public decimal Quantity { get; }
    public string TenantId { get; }

    public OrderCreatedEvent(Guid orderId, Guid userId, string assetCode, decimal quantity, string tenantId)
    {
        OrderId = orderId;
        UserId = userId;
        AssetCode = assetCode;
        Quantity = quantity;
        TenantId = tenantId;
    }
}