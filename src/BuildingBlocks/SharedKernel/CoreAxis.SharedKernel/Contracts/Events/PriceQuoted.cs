using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class PriceQuoted : IntegrationEvent
{
    public Guid OrderId { get; }
    public string AssetCode { get; }
    public decimal Quantity { get; }
    public decimal Price { get; }
    public DateTime Timestamp { get; }
    public string ProviderId { get; }
    public int ExpiresInSeconds { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public PriceQuoted(Guid orderId, string assetCode, decimal quantity, decimal price, 
        DateTime timestamp, string providerId, int expiresInSeconds, string tenantId,
        Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        OrderId = orderId;
        AssetCode = assetCode;
        Quantity = quantity;
        Price = price;
        Timestamp = timestamp;
        ProviderId = providerId;
        ExpiresInSeconds = expiresInSeconds;
        TenantId = tenantId;
    }
}