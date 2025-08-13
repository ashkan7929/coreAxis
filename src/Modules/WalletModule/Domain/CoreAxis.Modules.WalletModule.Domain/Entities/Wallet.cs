using CoreAxis.SharedKernel;
using CoreAxis.Modules.WalletModule.Domain.Events;

namespace CoreAxis.Modules.WalletModule.Domain.Entities;

public class Wallet : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid WalletTypeId { get; private set; }
    public decimal Balance { get; private set; } = 0;
    public string Currency { get; private set; } = "USD";
    public bool IsLocked { get; private set; } = false;
    public string? LockReason { get; private set; }
    public byte[] RowVersion { get; private set; } = new byte[0];

    // Navigation properties
    public virtual WalletType WalletType { get; private set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();
    public virtual ICollection<WalletContract> WalletContracts { get; private set; } = new List<WalletContract>();

    private Wallet() { } // For EF Core

    public Wallet(Guid userId, Guid walletTypeId, string currency = "USD")
    {
        UserId = userId;
        WalletTypeId = walletTypeId;
        Currency = currency;
        CreatedOn = DateTime.UtcNow;
        
        AddDomainEvent(new WalletCreatedEvent(Id, userId, walletTypeId));
    }

    public void Credit(decimal amount, string reason = "")
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
        
        if (IsLocked)
            throw new InvalidOperationException($"Wallet is locked: {LockReason}");

        Balance += amount;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new WalletBalanceChangedEvent(Id, Balance, amount, "CREDIT", reason));
    }

    public void Debit(decimal amount, string reason = "")
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
        
        if (IsLocked)
            throw new InvalidOperationException($"Wallet is locked: {LockReason}");
        
        if (Balance < amount)
            throw new InvalidOperationException("Insufficient balance");

        Balance -= amount;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new WalletBalanceChangedEvent(Id, Balance, -amount, "DEBIT", reason));
    }

    public void Lock(string reason)
    {
        IsLocked = true;
        LockReason = reason;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void Unlock()
    {
        IsLocked = false;
        LockReason = null;
        LastModifiedOn = DateTime.UtcNow;
    }

    public bool CanDebit(decimal amount)
    {
        return !IsLocked && Balance >= amount;
    }
}