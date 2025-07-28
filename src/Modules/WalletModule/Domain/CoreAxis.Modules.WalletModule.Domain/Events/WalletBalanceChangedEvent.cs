using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.WalletModule.Domain.Events;

public class WalletBalanceChangedEvent : DomainEvent
{
    public Guid WalletId { get; }
    public decimal NewBalance { get; }
    public decimal AmountChanged { get; }
    public string OperationType { get; }
    public string Reason { get; }

    public WalletBalanceChangedEvent(Guid walletId, decimal newBalance, decimal amountChanged, string operationType, string reason)
    {
        WalletId = walletId;
        NewBalance = newBalance;
        AmountChanged = amountChanged;
        OperationType = operationType;
        Reason = reason;
    }
}