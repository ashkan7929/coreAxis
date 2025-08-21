using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a line item in a refund request, detailing specific products/services being refunded.
/// </summary>
public class RefundLineItem : EntityBase
{
    /// <summary>
    /// Gets or sets the refund request ID this line item belongs to.
    /// </summary>
    [Required]
    public Guid RefundRequestId { get; set; }

    /// <summary>
    /// Gets or sets the original order line item ID being refunded.
    /// </summary>
    [Required]
    public Guid OrderLineItemId { get; set; }

    /// <summary>
    /// Gets or sets the product ID being refunded.
    /// </summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Gets or sets the product name at the time of refund.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product SKU at the time of refund.
    /// </summary>
    [MaxLength(100)]
    public string? ProductSku { get; set; }

    /// <summary>
    /// Gets or sets the original quantity ordered.
    /// </summary>
    [Required]
    public int OriginalQuantity { get; set; }

    /// <summary>
    /// Gets or sets the quantity being refunded.
    /// </summary>
    [Required]
    public int RefundQuantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price at the time of original purchase.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the total amount for this line item refund.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Gets or sets the tax amount being refunded for this line item.
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the discount amount being refunded for this line item.
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal DiscountAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the net refund amount for this line item (after taxes and discounts).
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal NetRefundAmount { get; set; }

    /// <summary>
    /// Gets or sets the reason for refunding this specific item.
    /// </summary>
    [MaxLength(500)]
    public string? ItemRefundReason { get; set; }

    /// <summary>
    /// Gets or sets whether this item needs to be returned physically.
    /// </summary>
    public bool RequiresReturn { get; set; } = false;

    /// <summary>
    /// Gets or sets the condition of the returned item.
    /// </summary>
    [MaxLength(200)]
    public string? ReturnCondition { get; set; }

    /// <summary>
    /// Gets or sets whether the item has been received back (if return required).
    /// </summary>
    public bool IsReturned { get; set; } = false;

    /// <summary>
    /// Gets or sets when the item was returned.
    /// </summary>
    public DateTime? ReturnedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for this line item as JSON.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Navigation property to the refund request.
    /// </summary>
    public virtual RefundRequest RefundRequest { get; set; } = null!;

    /// <summary>
    /// Calculates the total refund amount for this line item.
    /// </summary>
    public void CalculateRefundAmount()
    {
        RefundAmount = (UnitPrice * RefundQuantity) - DiscountAmount;
        NetRefundAmount = RefundAmount + TaxAmount;
    }

    /// <summary>
    /// Marks the item as returned.
    /// </summary>
    public void MarkAsReturned(string? condition = null)
    {
        if (!RequiresReturn)
            throw new InvalidOperationException("This item does not require return.");

        IsReturned = true;
        ReturnedAt = DateTime.UtcNow;
        ReturnCondition = condition;
    }

    /// <summary>
    /// Validates that the refund quantity doesn't exceed available quantity.
    /// </summary>
    public bool IsValidRefundQuantity(int alreadyRefundedQuantity = 0)
    {
        return RefundQuantity > 0 && 
               (RefundQuantity + alreadyRefundedQuantity) <= OriginalQuantity;
    }

    /// <summary>
    /// Gets the remaining quantity that can be refunded.
    /// </summary>
    public int GetRemainingRefundableQuantity(int alreadyRefundedQuantity = 0)
    {
        return Math.Max(0, OriginalQuantity - alreadyRefundedQuantity);
    }
}