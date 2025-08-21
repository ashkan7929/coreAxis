using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

/// <summary>
/// Interface for the pricing service.
/// </summary>
public interface IPricingService
{
    Task<PricingResult> ApplyDiscountsAsync(
        OrderSnapshot orderSnapshot,
        List<string>? couponCodes = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot of order for pricing calculation.
/// </summary>
public class OrderSnapshot
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItemSnapshot> Items { get; set; } = new();
    public decimal SubtotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Snapshot of order item for pricing.
/// </summary>
public class OrderItemSnapshot
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public List<Guid>? CategoryIds { get; set; }
}

/// <summary>
/// Result of pricing calculation.
/// </summary>
public class PricingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public BasePricing? BasePricing { get; set; }
    public List<AppliedDiscount> AppliedDiscounts { get; set; } = new();
    public FinalPricing? FinalPricing { get; set; }
    public List<ValidCoupon> ValidCoupons { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Base pricing without discounts.
/// </summary>
public class BasePricing
{
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<LineItemPricing> LineItemPricing { get; set; } = new();
}

/// <summary>
/// Final pricing after discounts.
/// </summary>
public class FinalPricing
{
    public decimal SubtotalAmount { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<AppliedDiscount> AppliedDiscounts { get; set; } = new();
}

/// <summary>
/// Pricing information for a line item.
/// </summary>
public class LineItemPricing
{
    public Guid LineItemId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal TaxAmount { get; set; }
}

/// <summary>
/// Applied discount information.
/// </summary>
public record AppliedDiscount
{
    public Guid DiscountId { get; init; }
    public string DiscountName { get; init; } = string.Empty;
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal AppliedToAmount { get; init; }
    public int Priority { get; init; }
    public string? CouponCode { get; set; }
}

/// <summary>
/// Valid coupon information.
/// </summary>
public class ValidCoupon
{
    public string CouponCode { get; set; } = string.Empty;
    public DiscountRule DiscountRule { get; set; } = null!;
    public CouponValidationResult ValidationResult { get; set; } = null!;
}

/// <summary>
/// Coupon validation result.
/// </summary>
public class CouponValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static CouponValidationResult Valid() => new() { IsValid = true };
    public static CouponValidationResult Invalid(string errorMessage) => 
        new() { IsValid = false, ErrorMessage = errorMessage };
}