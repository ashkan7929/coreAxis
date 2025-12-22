using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class PaymentFailed : IntegrationEvent
{
    public Guid OrderId { get; }
    public Guid PaymentId { get; }
    public string Reason { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public PaymentFailed(Guid orderId, Guid paymentId, string reason, string tenantId,
        Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        OrderId = orderId;
        PaymentId = paymentId;
        Reason = reason;
        TenantId = tenantId;
    }
}
