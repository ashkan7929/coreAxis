using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a processed refund for a payment.
/// </summary>
public class Refund : EntityBase
{
    /// <summary>
    /// Gets or sets the payment ID this refund belongs to.
    /// </summary>
    [Required]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Navigation property to the related payment.
    /// </summary>
    public virtual Payment? Payment { get; set; }

    /// <summary>
    /// Gets or sets the refund amount.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency for the refund.
    /// </summary>
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the refund reason (free text).
    /// </summary>
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refund status.
    /// </summary>
    [Required]
    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    /// <summary>
    /// Gets or sets the transaction ID provided by the gateway.
    /// </summary>
    [MaxLength(100)]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Some consumers refer to the gateway transaction as RefundTransactionId.
    /// Keeping for compatibility with existing queries.
    /// </summary>
    [MaxLength(100)]
    public string? RefundTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the gateway-specific transaction identifier.
    /// </summary>
    [MaxLength(100)]
    public string? GatewayTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the gateway response payload.
    /// </summary>
    [MaxLength(2000)]
    public string? GatewayResponse { get; set; }

    /// <summary>
    /// Gets or sets the failure reason if the refund failed.
    /// </summary>
    [MaxLength(1000)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets when the refund was processed.
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional idempotency key to prevent duplicate processing.
    /// </summary>
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Optional error message for consumer display.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Created timestamp used by existing EF configuration.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Updated timestamp used by existing EF configuration.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}