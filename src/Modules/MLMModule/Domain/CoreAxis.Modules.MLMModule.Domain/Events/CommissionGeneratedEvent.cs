using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.MLMModule.Domain.Events;

public class CommissionGeneratedEvent : DomainEvent
{
    public Guid CommissionId { get; }
    public Guid UserId { get; }
    public decimal Amount { get; }
    public int Level { get; }
    public Guid SourcePaymentId { get; }
    
    public CommissionGeneratedEvent(Guid commissionId, Guid userId, decimal amount, int level, Guid sourcePaymentId)
    {
        CommissionId = commissionId;
        UserId = userId;
        Amount = amount;
        Level = level;
        SourcePaymentId = sourcePaymentId;
    }
}