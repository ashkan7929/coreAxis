using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

/// <summary>
/// Integration event published when a price is quoted.
/// Preferred type for cross-service messaging.
/// </summary>
/// <remarks>
/// Naming guidance:
/// - As part of contracts cleanup, this will be renamed to <c>PriceQuotedIntegrationEvent</c>.
/// - The legacy DTO <c>Contracts.PriceQuoted</c> is marked [Obsolete] and will be renamed to <c>PriceQuotedDto</c>.
/// </remarks>
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