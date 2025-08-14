using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class OrderPlaced : IntegrationEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public string AssetCode { get; }
    public decimal Quantity { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";
    public Dictionary<string, object> Metadata { get; }

    public OrderPlaced(Guid orderId, Guid userId, string assetCode, decimal quantity, string tenantId, 
        Dictionary<string, object> metadata, Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        OrderId = orderId;
        UserId = userId;
        AssetCode = assetCode;
        Quantity = quantity;
        TenantId = tenantId;
        Metadata = metadata;
    }
}