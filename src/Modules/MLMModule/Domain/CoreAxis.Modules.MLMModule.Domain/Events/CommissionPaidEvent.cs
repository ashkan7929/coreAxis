using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.MLMModule.Domain.Events;

public class CommissionPaidEvent : DomainEvent
{
    public Guid CommissionId { get; }
    public Guid UserId { get; }
    public decimal Amount { get; }
    public Guid WalletTransactionId { get; }
    public Guid? TenantId { get; }
    
    public CommissionPaidEvent(Guid commissionId, Guid userId, decimal amount, Guid walletTransactionId, Guid? tenantId)
    {
        CommissionId = commissionId;
        UserId = userId;
        Amount = amount;
        WalletTransactionId = walletTransactionId;
        TenantId = tenantId;
    }
}