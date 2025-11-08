using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Products;

public class ProductListItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal Count { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public Money? PriceFrom { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
}