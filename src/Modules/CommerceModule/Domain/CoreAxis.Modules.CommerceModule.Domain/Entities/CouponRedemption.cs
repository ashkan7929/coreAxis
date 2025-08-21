using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a coupon redemption record tracking when and how coupons are used.
/// </summary>
public class CouponRedemption : EntityBase
{
    /// <summary>
    /// Gets or sets the ID of the discount rule associated with this redemption.
    /// </summary>
    public Guid DiscountRuleId { get; set; }

    /// <summary>
    /// Gets or sets the discount rule associated with this redemption.
    /// </summary>
    public virtual DiscountRule DiscountRule { get; set; } = null!;

    /// <summary>
    /// Gets or sets the coupon code that was redeemed.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string CouponCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user who redeemed the coupon.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the customer who redeemed the coupon.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the order where this coupon was applied.
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the original order amount before discount.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal OriginalAmount { get; set; }

    /// <summary>
    /// Gets or sets the discount amount that was applied.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Gets or sets the final amount after discount.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal FinalAmount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the coupon was redeemed.
    /// </summary>
    public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this redemption is still valid (not refunded/cancelled).
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Gets or sets the IP address from which the coupon was redeemed.
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string from the redemption request.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the redemption in JSON format.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for tracking related operations.
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the idempotency key to prevent duplicate redemptions.
    /// </summary>
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Marks this redemption as invalid (e.g., due to refund or cancellation).
    /// </summary>
    public void Invalidate()
    {
        IsValid = false;
    }

    /// <summary>
    /// Calculates the discount percentage applied.
    /// </summary>
    public decimal GetDiscountPercentage()
    {
        if (OriginalAmount == 0) return 0;
        return (DiscountAmount / OriginalAmount) * 100;
    }
}