using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents an allocation of payment amount to a specific party based on split payment rules.
/// </summary>
public class SplitPaymentAllocation : EntityBase
{
    /// <summary>
    /// Gets or sets the ID of the split payment rule that generated this allocation.
    /// </summary>
    [Required]
    public Guid SplitPaymentRuleId { get; set; }

    /// <summary>
    /// Gets or sets the order ID this allocation is associated with.
    /// </summary>
    [Required]
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the payment ID this allocation is part of.
    /// </summary>
    public Guid? PaymentId { get; set; }

    /// <summary>
    /// Gets or sets the party ID (vendor, marketplace, etc.) receiving this allocation.
    /// </summary>
    [Required]
    public Guid PartyId { get; set; }

    /// <summary>
    /// Gets or sets the type of party receiving this allocation.
    /// </summary>
    [Required]
    public SplitPaymentPartyType PartyType { get; set; }

    /// <summary>
    /// Gets or sets the allocation type (percentage, fixed amount, etc.).
    /// </summary>
    [Required]
    public SplitPaymentAllocationType AllocationType { get; set; }

    /// <summary>
    /// Gets or sets the allocation value (percentage or fixed amount).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal AllocationValue { get; set; }

    /// <summary>
    /// Gets or sets the calculated amount for this allocation.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal AllocatedAmount { get; set; }

    /// <summary>
    /// Gets or sets the currency of the allocated amount.
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original order amount this allocation is based on.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal BaseAmount { get; set; }

    /// <summary>
    /// Gets or sets the status of this allocation.
    /// </summary>
    [Required]
    public SplitPaymentAllocationStatus Status { get; set; } = SplitPaymentAllocationStatus.Pending;

    /// <summary>
    /// Gets or sets when this allocation was processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets when this allocation was settled/paid out.
    /// </summary>
    public DateTime? SettledAt { get; set; }

    /// <summary>
    /// Gets or sets the external transaction ID for this allocation.
    /// </summary>
    [MaxLength(100)]
    public string? ExternalTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the fee amount deducted from this allocation.
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal FeeAmount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the net amount after fees.
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Gets or sets any error message if allocation failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts for this allocation.
    /// </summary>
    public int RetryAttempts { get; set; } = 0;

    /// <summary>
    /// Gets or sets the next retry date if allocation failed.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

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
    /// Navigation property to the split payment rule.
    /// </summary>
    public virtual SplitPaymentRule SplitPaymentRule { get; set; } = null!;

    /// <summary>
    /// Marks this allocation as processed.
    /// </summary>
    public void MarkAsProcessed(string? externalTransactionId = null)
    {
        Status = SplitPaymentAllocationStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        ExternalTransactionId = externalTransactionId;
        ErrorMessage = null;
    }

    /// <summary>
    /// Marks this allocation as settled.
    /// </summary>
    public void MarkAsSettled()
    {
        Status = SplitPaymentAllocationStatus.Settled;
        SettledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this allocation as failed with an error message.
    /// </summary>
    public void MarkAsFailed(string errorMessage, DateTime? nextRetryAt = null)
    {
        Status = SplitPaymentAllocationStatus.Failed;
        ErrorMessage = errorMessage;
        RetryAttempts++;
        NextRetryAt = nextRetryAt;
    }

    /// <summary>
    /// Calculates the net amount after deducting fees.
    /// </summary>
    public void CalculateNetAmount()
    {
        NetAmount = AllocatedAmount - FeeAmount;
    }

    /// <summary>
    /// Checks if this allocation can be retried.
    /// </summary>
    public bool CanRetry(int maxRetryAttempts = 3)
    {
        return Status == SplitPaymentAllocationStatus.Failed && 
               RetryAttempts < maxRetryAttempts &&
               (!NextRetryAt.HasValue || DateTime.UtcNow >= NextRetryAt.Value);
    }
}