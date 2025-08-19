using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class CommissionGenerated : IntegrationEvent
{
    public Guid SourcePaymentId { get; }
    public Guid UserId { get; }
    public decimal Amount { get; }
    public int Level { get; }
    public decimal Percentage { get; }
    public decimal SourceAmount { get; }
    public string UserName { get; }
    public string UserEmail { get; }
    public string RuleSetName { get; }
    public int RuleSetVersion { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public CommissionGenerated(Guid sourcePaymentId, Guid userId, decimal amount, int level, 
        decimal percentage, decimal sourceAmount, string userName, string userEmail,
        string ruleSetName, int ruleSetVersion, string tenantId,
        Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        SourcePaymentId = sourcePaymentId;
        UserId = userId;
        Amount = amount;
        Level = level;
        Percentage = percentage;
        SourceAmount = sourceAmount;
        UserName = userName;
        UserEmail = userEmail;
        RuleSetName = ruleSetName;
        RuleSetVersion = ruleSetVersion;
        TenantId = tenantId;
    }
}