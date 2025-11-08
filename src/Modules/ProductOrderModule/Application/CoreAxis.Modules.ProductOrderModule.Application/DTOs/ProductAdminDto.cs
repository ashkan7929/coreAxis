using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;

namespace CoreAxis.Modules.ProductOrderModule.Application.DTOs;

public class ProductAdminDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public decimal? PriceFrom { get; set; }
    public string? Currency { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
    public Guid? SupplierId { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}

public class CreateProductRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public decimal? PriceFrom { get; set; }
    public string? Currency { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
    public Guid? SupplierId { get; set; }
}

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public decimal? PriceFrom { get; set; }
    public string? Currency { get; set; }
    public decimal Count { get; set; }
    public decimal? Quantity { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
    public Guid? SupplierId { get; set; }
}