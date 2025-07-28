using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.WalletModule.Domain.Events;

public class TransactionCreatedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public Guid WalletId { get; }
    public decimal Amount { get; }
    public Guid TransactionTypeId { get; }
    public Guid TenantId { get; }

    public TransactionCreatedEvent(Guid transactionId, Guid walletId, decimal amount, Guid transactionTypeId, Guid tenantId)
    {
        TransactionId = transactionId;
        WalletId = walletId;
        Amount = amount;
        TransactionTypeId = transactionTypeId;
        TenantId = tenantId;
    }
}

public class TransactionCompletedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public Guid WalletId { get; }
    public decimal Amount { get; }
    public Guid TransactionTypeId { get; }

    public TransactionCompletedEvent(Guid transactionId, Guid walletId, decimal amount, Guid transactionTypeId)
    {
        TransactionId = transactionId;
        WalletId = walletId;
        Amount = amount;
        TransactionTypeId = transactionTypeId;
    }
}

public class TransactionFailedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public Guid WalletId { get; }
    public decimal Amount { get; }
    public string Reason { get; }

    public TransactionFailedEvent(Guid transactionId, Guid walletId, decimal amount, string reason)
    {
        TransactionId = transactionId;
        WalletId = walletId;
        Amount = amount;
        Reason = reason;
    }
}