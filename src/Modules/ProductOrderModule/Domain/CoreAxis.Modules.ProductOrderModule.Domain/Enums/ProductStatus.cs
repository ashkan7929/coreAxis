namespace CoreAxis.Modules.ProductOrderModule.Domain.Enums;

/// <summary>
/// Represents the status of a product.
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product is active and visible to public listings.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Product is inactive and hidden from public listings.
    /// </summary>
    Inactive = 1
}