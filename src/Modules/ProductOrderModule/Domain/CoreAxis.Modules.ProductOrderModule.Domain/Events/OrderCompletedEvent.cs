using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Events;

/// <summary>
/// Domain event raised when an order is completed.
/// </summary>
public class OrderCompletedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public decimal TotalAmount { get; }
    public string TenantId { get; }

    public OrderCompletedEvent(Guid orderId, Guid userId, decimal totalAmount, string tenantId)
    {
        OrderId = orderId;
        UserId = userId;
        TotalAmount = totalAmount;
        TenantId = tenantId;
    }
}