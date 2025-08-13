using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts;

public class PriceQuoted : IntegrationEvent
{
    public Guid OrderId { get; }
    public string AssetCode { get; }
    public decimal Quantity { get; }
    public decimal Price { get; }
    public string Currency { get; }
    public DateTime QuoteTimestamp { get; }
    public DateTime ExpiresAt { get; }
    public string QuoteId { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; }
    public Dictionary<string, object> Metadata { get; }

    public PriceQuoted(
        Guid orderId,
        string assetCode,
        decimal quantity,
        decimal price,
        string currency,
        DateTime quoteTimestamp,
        DateTime expiresAt,
        string quoteId,
        string tenantId,
        string schemaVersion = "v1",
        Dictionary<string, object>? metadata = null)
        : base(Guid.NewGuid(), DateTime.UtcNow)
    {
        OrderId = orderId;
        AssetCode = assetCode;
        Quantity = quantity;
        Price = price;
        Currency = currency;
        QuoteTimestamp = quoteTimestamp;
        ExpiresAt = expiresAt;
        QuoteId = quoteId;
        TenantId = tenantId;
        SchemaVersion = schemaVersion;
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}