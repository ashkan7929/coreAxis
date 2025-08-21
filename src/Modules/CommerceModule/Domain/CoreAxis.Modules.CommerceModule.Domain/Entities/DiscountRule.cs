using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a discount rule that can be applied to products, categories, or entire cart.
/// </summary>
public class DiscountRule : EntityBase
{
    /// <summary>
    /// Gets or sets the name of the discount rule.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the discount rule.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the scope of the discount (Product, Category, Cart).
    /// </summary>
    public DiscountScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the type of discount calculation (Percent, Fixed).
    /// </summary>
    public DiscountType Type { get; set; }

    /// <summary>
    /// Gets or sets the discount value (percentage or fixed amount).
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the maximum discount amount (for percentage discounts).
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxDiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the minimum order amount required for this discount.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinOrderAmount { get; set; }

    /// <summary>
    /// Gets or sets the start date when this discount becomes active.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date when this discount expires.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets whether this discount is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of times this discount can be used.
    /// </summary>
    public int? UsageLimit { get; set; }

    /// <summary>
    /// Gets or sets the current usage count of this discount.
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum number of times this discount can be used per customer.
    /// </summary>
    public int? UsageLimitPerCustomer { get; set; }

    /// <summary>
    /// Gets or sets the priority of this discount rule (higher number = higher priority).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this discount can be combined with other discounts.
    /// </summary>
    public bool CanCombine { get; set; } = true;

    /// <summary>
    /// Gets or sets the JSON string containing specific conditions for this discount.
    /// This can include product IDs, category IDs, customer groups, etc.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ConditionsJson { get; set; }

    /// <summary>
    /// Gets or sets the coupon code required to activate this discount (if any).
    /// </summary>
    [MaxLength(50)]
    public string? CouponCode { get; set; }

    /// <summary>
    /// Gets or sets whether a coupon code is required for this discount.
    /// </summary>
    public bool RequiresCoupon { get; set; } = false;

    /// <summary>
    /// Checks if the discount is currently valid based on date range and active status.
    /// </summary>
    public bool IsCurrentlyValid()
    {
        if (!IsActive) return false;
        
        var now = DateTime.UtcNow;
        
        if (StartDate.HasValue && now < StartDate.Value) return false;
        if (EndDate.HasValue && now > EndDate.Value) return false;
        
        if (UsageLimit.HasValue && UsageCount >= UsageLimit.Value) return false;
        
        return true;
    }

    /// <summary>
    /// Checks if the discount has reached its usage limit.
    /// </summary>
    public bool HasReachedUsageLimit()
    {
        return UsageLimit.HasValue && UsageCount >= UsageLimit.Value;
    }

    /// <summary>
    /// Increments the usage count for this discount rule.
    /// </summary>
    public void IncrementUsage()
    {
        UsageCount++;
    }
}