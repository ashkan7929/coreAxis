using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.MLMModule.Domain.Events;

public class CommissionExpiredEvent : DomainEvent
{
    public Guid CommissionId { get; }
    public Guid UserId { get; }
    public decimal Amount { get; }
    
    public CommissionExpiredEvent(Guid commissionId, Guid userId, decimal amount)
    {
        CommissionId = commissionId;
        UserId = userId;
        Amount = amount;
    }
}