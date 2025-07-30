using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.MLMModule.Domain.Events;

public class PaymentConfirmedEvent : DomainEvent
{
    public Guid UserId { get; }
    public decimal Amount { get; }
    public Guid PaymentId { get; }
    public Guid? ProductId { get; }
    public DateTime PaymentDate { get; }
    
    public PaymentConfirmedEvent(Guid userId, decimal amount, Guid paymentId, Guid? productId = null, DateTime? paymentDate = null)
    {
        UserId = userId;
        Amount = amount;
        PaymentId = paymentId;
        ProductId = productId;
        PaymentDate = paymentDate ?? DateTime.UtcNow;
    }
}