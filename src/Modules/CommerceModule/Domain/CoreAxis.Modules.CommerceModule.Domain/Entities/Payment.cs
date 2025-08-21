using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a payment transaction for an order.
/// </summary>
public class Payment : EntityBase
{
    /// <summary>
    /// Gets or sets the order ID this payment is associated with.
    /// </summary>
    [Required]
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the order this payment belongs to.
    /// </summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>
    /// Gets or sets the payment amount.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code (e.g., USD, EUR).
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the payment status.
    /// </summary>
    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Gets or sets the payment method used.
    /// </summary>
    [Required]
    public PaymentMethod Method { get; set; }

    /// <summary>
    /// Gets or sets the external transaction ID from payment gateway.
    /// </summary>
    [MaxLength(100)]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the payment gateway reference.
    /// </summary>
    [MaxLength(100)]
    public string? GatewayReference { get; set; }

    /// <summary>
    /// Gets or sets the reference number for this payment.
    /// </summary>
    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the payment gateway used.
    /// </summary>
    [MaxLength(50)]
    public string? Gateway { get; set; }

    /// <summary>
    /// Gets or sets the date when payment was processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the date when payment failed (if applicable).
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Gets or sets the failure reason (if payment failed).
    /// </summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets additional payment metadata as JSON.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the customer ID who made the payment.
    /// </summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the refunded amount (if any).
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal RefundedAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this payment has been refunded.
    /// </summary>
    public bool IsRefunded { get; set; } = false;

    /// <summary>
    /// Gets or sets the correlation ID for tracking related operations.
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the collection of refund requests for this payment.
    /// </summary>
    public virtual ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();

    /// <summary>
    /// Gets or sets the collection of split payment allocations.
    /// </summary>
    public virtual ICollection<SplitPaymentAllocation> SplitAllocations { get; set; } = new List<SplitPaymentAllocation>();

    /// <summary>
    /// Calculates the remaining refundable amount.
    /// </summary>
    public decimal GetRefundableAmount()
    {
        return Math.Max(0, Amount - RefundedAmount);
    }

    /// <summary>
    /// Checks if the payment can be refunded.
    /// </summary>
    public bool CanBeRefunded()
    {
        return Status == PaymentStatus.Completed && GetRefundableAmount() > 0;
    }

    /// <summary>
    /// Marks the payment as processed.
    /// </summary>
    public void MarkAsProcessed(string? transactionId = null, string? gatewayReference = null)
    {
        Status = PaymentStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
        TransactionId = transactionId;
        GatewayReference = gatewayReference;
    }

    /// <summary>
    /// Marks the payment as failed.
    /// </summary>
    public void MarkAsFailed(string? reason = null)
    {
        Status = PaymentStatus.Failed;
        FailedAt = DateTime.UtcNow;
        FailureReason = reason;
    }
}