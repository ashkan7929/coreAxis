using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class CommissionCalculated : IntegrationEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public decimal OrderAmount { get; }
    public decimal CommissionAmount { get; }
    public decimal CommissionRate { get; }
    public string CommissionType { get; }
    public Dictionary<string, decimal> Breakdown { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public CommissionCalculated(Guid orderId, Guid userId, decimal orderAmount, decimal commissionAmount,
        decimal commissionRate, string commissionType, Dictionary<string, decimal> breakdown, string tenantId,
        Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow)
    {
        OrderId = orderId;
        UserId = userId;
        OrderAmount = orderAmount;
        CommissionAmount = commissionAmount;
        CommissionRate = commissionRate;
        CommissionType = commissionType;
        Breakdown = breakdown;
        TenantId = tenantId;
    }
}