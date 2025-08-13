namespace CoreAxis.SharedKernel.Ports;

public interface IPriceProvider
{
    Task<PriceQuote> GetQuoteAsync(string assetCode, decimal quantity, PriceContext context, CancellationToken cancellationToken = default);
}

public class PriceQuote
{
    public decimal Price { get; }
    public DateTime Timestamp { get; }
    public string ProviderId { get; }
    public int ExpiresInSeconds { get; }
    public string AssetCode { get; }
    public decimal Quantity { get; }

    public PriceQuote(decimal price, DateTime timestamp, string providerId, int expiresInSeconds, string assetCode, decimal quantity)
    {
        Price = price;
        Timestamp = timestamp;
        ProviderId = providerId;
        ExpiresInSeconds = expiresInSeconds;
        AssetCode = assetCode;
        Quantity = quantity;
    }
}

public class PriceContext
{
    public string TenantId { get; }
    public Guid UserId { get; }
    public Guid CorrelationId { get; }
    public Dictionary<string, object> Metadata { get; }

    public PriceContext(string tenantId, Guid userId, Guid correlationId, Dictionary<string, object>? metadata = null)
    {
        TenantId = tenantId;
        UserId = userId;
        CorrelationId = correlationId;
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}