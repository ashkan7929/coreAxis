using CoreAxis.EventBus;

namespace CoreAxis.Modules.ProductOrderModule.Application.Events;

public class ProductUpdated : IntegrationEvent
{
    public Guid ProductId { get; }
    public string Code { get; }
    public string Name { get; }
    public decimal BasePriceAmount { get; }
    public string Currency { get; }
    public Dictionary<string, string> Attributes { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public ProductUpdated(Guid productId, string code, string name, decimal basePriceAmount, string currency,
        Dictionary<string, string> attributes, string tenantId, Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        ProductId = productId;
        Code = code;
        Name = name;
        BasePriceAmount = basePriceAmount;
        Currency = currency;
        Attributes = attributes;
        TenantId = tenantId;
    }
}

public class ProductStatusChanged : IntegrationEvent
{
    public Guid ProductId { get; }
    public string Code { get; }
    public string Status { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public ProductStatusChanged(Guid productId, string code, string status, string tenantId,
        Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        ProductId = productId;
        Code = code;
        Status = status;
        TenantId = tenantId;
    }
}