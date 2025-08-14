using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class PaymentInitiated : IntegrationEvent
{
    public Guid OrderId { get; }
    public Guid PaymentId { get; }
    public Guid UserId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string PaymentMethod { get; }
    public string IdempotencyKey { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public PaymentInitiated(Guid orderId, Guid paymentId, Guid userId, decimal amount, 
        string currency, string paymentMethod, string idempotencyKey, string tenantId,
        Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        OrderId = orderId;
        PaymentId = paymentId;
        UserId = userId;
        Amount = amount;
        Currency = currency;
        PaymentMethod = paymentMethod;
        IdempotencyKey = idempotencyKey;
        TenantId = tenantId;
    }
}