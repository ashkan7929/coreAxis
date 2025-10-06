using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;

namespace CoreAxis.Modules.ProductOrderModule.Application.DTOs;

public class ProductPricingInput
{
    public Guid ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Money? BasePrice { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
}