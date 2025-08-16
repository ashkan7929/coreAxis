using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Events;

/// <summary>
/// Domain event raised when an order expires due to price lock expiry.
/// </summary>
public class OrderExpiredEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public string TenantId { get; }

    public OrderExpiredEvent(Guid orderId, Guid userId, string tenantId)
    {
        OrderId = orderId;
        UserId = userId;
        TenantId = tenantId;
    }
}