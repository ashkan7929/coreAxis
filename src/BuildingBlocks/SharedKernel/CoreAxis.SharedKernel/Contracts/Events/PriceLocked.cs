using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class PriceLocked : IntegrationEvent
{
    public Guid OrderId { get; }
    public string AssetCode { get; }
    public decimal Quantity { get; }
    public decimal LockedPrice { get; }
    public DateTime LockedAt { get; }
    public DateTime ExpiresAt { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public PriceLocked(Guid orderId, string assetCode, decimal quantity, decimal lockedPrice,
        DateTime lockedAt, DateTime expiresAt, string tenantId,
        Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        OrderId = orderId;
        AssetCode = assetCode;
        Quantity = quantity;
        LockedPrice = lockedPrice;
        LockedAt = lockedAt;
        ExpiresAt = expiresAt;
        TenantId = tenantId;
    }
}