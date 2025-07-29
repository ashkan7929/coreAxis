using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.MLMModule.Domain.Events;

public class PaymentConfirmedEvent : DomainEvent
{
    public Guid UserId { get; }
    public decimal Amount { get; }
    public Guid PaymentId { get; }
    public Guid? ProductId { get; }
    public Guid? TenantId { get; }
    public DateTime PaymentDate { get; }
    
    public PaymentConfirmedEvent(Guid userId, decimal amount, Guid paymentId, Guid? tenantId, Guid? productId = null, DateTime? paymentDate = null)
    {
        UserId = userId;
        Amount = amount;
        PaymentId = paymentId;
        ProductId = productId;
        TenantId = tenantId;
        PaymentDate = paymentDate ?? DateTime.UtcNow;
    }
}