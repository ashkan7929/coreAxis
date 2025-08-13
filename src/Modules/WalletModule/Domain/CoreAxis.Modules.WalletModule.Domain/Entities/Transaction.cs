using CoreAxis.SharedKernel;
using CoreAxis.Modules.WalletModule.Domain.Events;
using System.Text.Json;

namespace CoreAxis.Modules.WalletModule.Domain.Entities;

public class Transaction : EntityBase
{
    public Guid WalletId { get; private set; }
    public Guid TransactionTypeId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Reference { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public Guid? CorrelationId { get; private set; }
    public string? Metadata { get; private set; }
    public TransactionStatus Status { get; private set; } = TransactionStatus.Pending;
    public DateTime ProcessedAt { get; private set; }
    public Guid? RelatedTransactionId { get; private set; } // For transfers

    // Navigation properties
    public virtual Wallet Wallet { get; private set; } = null!;
    public virtual TransactionType TransactionType { get; private set; } = null!;
    public virtual Transaction? RelatedTransaction { get; private set; }

    private Transaction() { } // For EF Core

    public Transaction(
        Guid walletId, 
        Guid transactionTypeId, 
        decimal amount, 
        decimal balanceAfter,
        string description, 
        string? reference = null,
        string? idempotencyKey = null,
        Guid? correlationId = null,
        object? metadata = null,
        Guid? relatedTransactionId = null)
    {
        WalletId = walletId;
        TransactionTypeId = transactionTypeId;
        Amount = amount;
        BalanceAfter = balanceAfter;
        Description = description;
        Reference = reference;
        IdempotencyKey = idempotencyKey;
        CorrelationId = correlationId;
        RelatedTransactionId = relatedTransactionId;
        ProcessedAt = DateTime.UtcNow;
        CreatedOn = DateTime.UtcNow;
        
        if (metadata != null)
        {
            Metadata = JsonSerializer.Serialize(metadata);
        }
        
        AddDomainEvent(new TransactionCreatedEvent(Id, walletId, amount, transactionTypeId));
    }

    public void Complete()
    {
        Status = TransactionStatus.Completed;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new TransactionCompletedEvent(Id, WalletId, Amount, TransactionTypeId));
    }

    public void Fail(string reason)
    {
        Status = TransactionStatus.Failed;
        Description += $" - Failed: {reason}";
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new TransactionFailedEvent(Id, WalletId, Amount, reason));
    }

    public void Cancel(string reason)
    {
        Status = TransactionStatus.Cancelled;
        Description += $" - Cancelled: {reason}";
        LastModifiedOn = DateTime.UtcNow;
    }

    public T? GetMetadata<T>() where T : class
    {
        if (string.IsNullOrEmpty(Metadata))
            return null;
            
        try
        {
            return JsonSerializer.Deserialize<T>(Metadata);
        }
        catch
        {
            return null;
        }
    }
}

public enum TransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3
}