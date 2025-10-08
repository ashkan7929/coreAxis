using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class CommissionBooked : IntegrationEvent
{
    public Guid CommissionId { get; }
    public Guid UserId { get; }
    public Guid WalletTransactionId { get; }
    public decimal Amount { get; }
    public int Level { get; }
    public string RuleSetCode { get; }
    public int RuleVersion { get; }
    public Guid? SourcePaymentId { get; }
    public Guid? ProductId { get; }
    public DateTime BookedAt { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public CommissionBooked(
        Guid commissionId,
        Guid userId,
        Guid walletTransactionId,
        decimal amount,
        int level,
        string ruleSetCode,
        int ruleVersion,
        Guid? sourcePaymentId,
        Guid? productId,
        DateTime bookedAt,
        string tenantId,
        Guid correlationId,
        Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        CommissionId = commissionId;
        UserId = userId;
        WalletTransactionId = walletTransactionId;
        Amount = amount;
        Level = level;
        RuleSetCode = ruleSetCode;
        RuleVersion = ruleVersion;
        SourcePaymentId = sourcePaymentId;
        ProductId = productId;
        BookedAt = bookedAt;
        TenantId = tenantId;
    }
}