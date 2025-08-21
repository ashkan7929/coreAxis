using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a refund request for an order or payment.
/// </summary>
public class RefundRequest : EntityBase
{
    /// <summary>
    /// Gets or sets the order ID this refund is associated with.
    /// </summary>
    [Required]
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the payment ID this refund is associated with.
    /// </summary>
    public Guid? PaymentId { get; set; }

    /// <summary>
    /// Gets or sets the refund request number (unique identifier for tracking).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RefundNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of refund.
    /// </summary>
    [Required]
    public RefundType RefundType { get; set; }

    /// <summary>
    /// Gets or sets the reason for the refund.
    /// </summary>
    [Required]
    public RefundReason Reason { get; set; }

    /// <summary>
    /// Gets or sets the detailed reason description.
    /// </summary>
    [MaxLength(1000)]
    public string? ReasonDescription { get; set; }

    /// <summary>
    /// Gets or sets the original payment amount.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal OriginalAmount { get; set; }

    /// <summary>
    /// Gets or sets the requested refund amount.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal RequestedAmount { get; set; }

    /// <summary>
    /// Gets or sets the approved refund amount.
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? ApprovedAmount { get; set; }

    /// <summary>
    /// Gets or sets the actual refunded amount.
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal RefundedAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the refund fee amount.
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal FeeAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the net refund amount (refunded amount - fees).
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal NetRefundAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the currency of the refund.
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the refund request.
    /// </summary>
    [Required]
    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    /// <summary>
    /// Gets or sets the user ID who requested the refund.
    /// </summary>
    public Guid? RequestedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when the refund was requested.
    /// </summary>
    [Required]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who approved the refund.
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// Gets or sets when the refund was approved.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Gets or sets when the refund was processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets when the refund was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the external refund ID from payment provider.
    /// </summary>
    [MaxLength(100)]
    public string? ExternalRefundId { get; set; }

    /// <summary>
    /// Gets or sets the payment provider used for the refund.
    /// </summary>
    [MaxLength(50)]
    public string? PaymentProvider { get; set; }

    /// <summary>
    /// Gets or sets the refund method (original payment method, wallet, etc.).
    /// </summary>
    [Required]
    public RefundMethod RefundMethod { get; set; }

    /// <summary>
    /// Gets or sets the destination account for the refund (if applicable).
    /// </summary>
    [MaxLength(100)]
    public string? RefundDestination { get; set; }

    /// <summary>
    /// Gets or sets any error message if refund failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryAttempts { get; set; } = 0;

    /// <summary>
    /// Gets or sets the next retry date if refund failed.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts allowed.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether this refund requires manual approval.
    /// </summary>
    public bool RequiresApproval { get; set; } = false;

    /// <summary>
    /// Gets or sets the priority of this refund request.
    /// </summary>
    public RefundPriority Priority { get; set; } = RefundPriority.Normal;

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for tracking related operations.
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the idempotency key to prevent duplicate refunds.
    /// </summary>
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Gets or sets internal notes about the refund.
    /// </summary>
    [MaxLength(2000)]
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Navigation property for refund line items.
    /// </summary>
    public virtual ICollection<RefundLineItem> LineItems { get; set; } = new List<RefundLineItem>();

    /// <summary>
    /// Approves the refund request.
    /// </summary>
    public void Approve(Guid approvedByUserId, decimal? approvedAmount = null)
    {
        if (Status != RefundStatus.Pending)
            throw new InvalidOperationException("Only pending refunds can be approved.");

        Status = RefundStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = DateTime.UtcNow;
        ApprovedAmount = approvedAmount ?? RequestedAmount;
    }

    /// <summary>
    /// Rejects the refund request.
    /// </summary>
    public void Reject(Guid rejectedByUserId, string reason)
    {
        if (Status != RefundStatus.Pending)
            throw new InvalidOperationException("Only pending refunds can be rejected.");

        Status = RefundStatus.Rejected;
        ApprovedByUserId = rejectedByUserId;
        ApprovedAt = DateTime.UtcNow;
        ReasonDescription = reason;
    }

    /// <summary>
    /// Marks the refund as processing.
    /// </summary>
    public void MarkAsProcessing()
    {
        if (Status != RefundStatus.Approved)
            throw new InvalidOperationException("Only approved refunds can be marked as processing.");

        Status = RefundStatus.Processing;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the refund as completed.
    /// </summary>
    public void MarkAsCompleted(decimal refundedAmount, string? externalRefundId = null)
    {
        if (Status != RefundStatus.Processing)
            throw new InvalidOperationException("Only processing refunds can be marked as completed.");

        Status = RefundStatus.Completed;
        RefundedAmount = refundedAmount;
        NetRefundAmount = refundedAmount - FeeAmount;
        ExternalRefundId = externalRefundId;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    /// <summary>
    /// Marks the refund as failed.
    /// </summary>
    public void MarkAsFailed(string errorMessage, DateTime? nextRetryAt = null)
    {
        Status = RefundStatus.Failed;
        ErrorMessage = errorMessage;
        RetryAttempts++;
        NextRetryAt = nextRetryAt;
    }

    /// <summary>
    /// Checks if the refund can be retried.
    /// </summary>
    public bool CanRetry()
    {
        return Status == RefundStatus.Failed &&
               RetryAttempts < MaxRetryAttempts &&
               (!NextRetryAt.HasValue || DateTime.UtcNow >= NextRetryAt.Value);
    }

    /// <summary>
    /// Calculates the maximum refundable amount.
    /// </summary>
    public decimal GetMaxRefundableAmount()
    {
        // This would typically consider previous refunds, fees, etc.
        return OriginalAmount - RefundedAmount;
    }

    /// <summary>
    /// Checks if the refund is in a final state.
    /// </summary>
    public bool IsInFinalState()
    {
        return Status is RefundStatus.Completed or RefundStatus.Rejected or RefundStatus.Cancelled;
    }
}