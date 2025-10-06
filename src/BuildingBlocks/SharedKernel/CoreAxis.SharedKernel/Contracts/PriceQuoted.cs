using System;
using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts;

/// <summary>
/// Legacy contract for price quotation payloads (DTO-style).
/// Prefer using <see cref="Contracts.Events.PriceQuoted"/> for integration messaging.
/// </summary>
/// <remarks>
/// Cleanup plan:
/// - If both a DTO and an integration event are required, this type will be
///   renamed to <c>PriceQuotedDto</c> and the event type to <c>PriceQuotedIntegrationEvent</c>.
/// - This class is kept temporarily to avoid breaking changes; migrate callers
///   to <see cref="Contracts.Events.PriceQuoted"/> and plan removal in the next major release.
/// </remarks>
[Obsolete("Use CoreAxis.SharedKernel.Contracts.Events.PriceQuoted. Planned rename: PriceQuotedDto in next release.")]
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