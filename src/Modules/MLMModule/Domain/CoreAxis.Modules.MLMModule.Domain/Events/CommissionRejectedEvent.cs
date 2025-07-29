using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.MLMModule.Domain.Events;

public class CommissionRejectedEvent : DomainEvent
{
    public Guid CommissionId { get; }
    public Guid UserId { get; }
    public decimal Amount { get; }
    public string RejectionReason { get; }
    public Guid? TenantId { get; }
    
    public CommissionRejectedEvent(Guid commissionId, Guid userId, decimal amount, string rejectionReason, Guid? tenantId)
    {
        CommissionId = commissionId;
        UserId = userId;
        Amount = amount;
        RejectionReason = rejectionReason;
        TenantId = tenantId;
    }
}