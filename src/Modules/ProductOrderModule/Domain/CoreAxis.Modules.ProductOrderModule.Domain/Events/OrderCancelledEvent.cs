using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Events;

/// <summary>
/// Domain event raised when an order is cancelled.
/// </summary>
public class OrderCancelledEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public string Reason { get; }
    public string TenantId { get; }

    public OrderCancelledEvent(Guid orderId, Guid userId, string reason, string tenantId)
    {
        OrderId = orderId;
        UserId = userId;
        Reason = reason;
        TenantId = tenantId;
    }
}