using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.WalletModule.Domain.Events;

public class WalletCreatedEvent : DomainEvent
{
    public Guid WalletId { get; }
    public Guid UserId { get; }
    public Guid WalletTypeId { get; }

    public WalletCreatedEvent(Guid walletId, Guid userId, Guid walletTypeId)
    {
        WalletId = walletId;
        UserId = userId;
        WalletTypeId = walletTypeId;
    }
}