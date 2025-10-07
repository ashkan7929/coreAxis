using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

/// <summary>
/// Integration event published when an order is finalized and eligible for commission processing.
/// </summary>
public class OrderFinalized : IntegrationEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public decimal TotalAmount { get; }
    public string Currency { get; }
    public DateTime FinalizedAt { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public OrderFinalized(
        Guid orderId,
        Guid userId,
        decimal totalAmount,
        string currency,
        DateTime finalizedAt,
        string tenantId,
        Guid correlationId,
        Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        OrderId = orderId;
        UserId = userId;
        TotalAmount = totalAmount;
        Currency = currency;
        FinalizedAt = finalizedAt;
        TenantId = tenantId;
    }
}