using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class PaymentConfirmed : IntegrationEvent
{
    public Guid OrderId { get; }
    public Guid PaymentId { get; }
    public Guid TransactionId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public DateTime ConfirmedAt { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public PaymentConfirmed(Guid orderId, Guid paymentId, Guid transactionId, decimal amount,
        string currency, DateTime confirmedAt, string tenantId,
        Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        OrderId = orderId;
        PaymentId = paymentId;
        TransactionId = transactionId;
        Amount = amount;
        Currency = currency;
        ConfirmedAt = confirmedAt;
        TenantId = tenantId;
    }
}