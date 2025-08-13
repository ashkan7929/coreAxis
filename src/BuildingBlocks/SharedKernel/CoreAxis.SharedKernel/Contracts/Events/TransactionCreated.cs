using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class TransactionCreated : IntegrationEvent
{
    public Guid TransactionId { get; }
    public Guid WalletId { get; }
    public Guid UserId { get; }
    public decimal Amount { get; }
    public decimal BalanceAfter { get; }
    public string TransactionType { get; }
    public string Description { get; }
    public string? Reference { get; }
    public string? IdempotencyKey { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public TransactionCreated(Guid transactionId, Guid walletId, Guid userId, decimal amount,
        decimal balanceAfter, string transactionType, string description, string? reference,
        string? idempotencyKey, string tenantId, Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow)
    {
        TransactionId = transactionId;
        WalletId = walletId;
        UserId = userId;
        Amount = amount;
        BalanceAfter = balanceAfter;
        TransactionType = transactionType;
        Description = description;
        Reference = reference;
        IdempotencyKey = idempotencyKey;
        TenantId = tenantId;
    }
}