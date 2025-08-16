using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Events;

/// <summary>
/// Domain event raised when an order is confirmed.
/// </summary>
public class OrderConfirmedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public string TenantId { get; }

    public OrderConfirmedEvent(Guid orderId, Guid userId, string tenantId)
    {
        OrderId = orderId;
        UserId = userId;
        TenantId = tenantId;
    }
}