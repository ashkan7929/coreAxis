using CoreAxis.SharedKernel;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Entities;

public class Product : EntityBase
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ProductStatus Status { get; private set; } = ProductStatus.Active;
    public Money? PriceFrom { get; private set; }
    public Dictionary<string, string> Attributes { get; private set; } = new();

    private Product() { }

    public static Product Create(string code, string name, ProductStatus status, Money? priceFrom = null, Dictionary<string, string>? attributes = null)
    {
        var product = new Product
        {
            Code = code.Trim(),
            Name = name.Trim(),
            Status = status,
            PriceFrom = priceFrom,
            Attributes = attributes ?? new Dictionary<string, string>()
        };

        return product;
    }

    public void Update(string name, ProductStatus status, Money? priceFrom = null, Dictionary<string, string>? attributes = null)
    {
        Name = name.Trim();
        Status = status;
        PriceFrom = priceFrom;
        Attributes = attributes ?? Attributes;
    }
}