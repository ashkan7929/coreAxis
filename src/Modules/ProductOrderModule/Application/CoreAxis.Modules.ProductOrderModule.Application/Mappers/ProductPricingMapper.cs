using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;

namespace CoreAxis.Modules.ProductOrderModule.Application.Mappers;

public static class ProductPricingMapper
{
    public static ProductPricingInput ToPricingInput(Product product)
    {
        return new ProductPricingInput
        {
            ProductId = product.Id,
            Code = product.Code,
            Name = product.Name,
            BasePrice = product.PriceFrom,
            Attributes = new Dictionary<string, string>(product.Attributes)
        };
    }
}