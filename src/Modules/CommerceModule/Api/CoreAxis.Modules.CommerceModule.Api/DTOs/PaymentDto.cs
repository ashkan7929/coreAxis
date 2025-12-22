using CoreAxis.Modules.CommerceModule.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.CommerceModule.Api.DTOs;

/// <summary>
/// DTO for payment information
/// </summary>
public class PaymentDto
{
    /// <summary>
    /// Gets or sets the payment ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the order ID
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the customer ID
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the payment method
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment status
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the transaction ID from payment provider
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the processed date
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the idempotency key
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Gets or sets the error message if payment failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for processing a payment
/// </summary>
public class ProcessPaymentDto
{
    /// <summary>
    /// Gets or sets the order ID
    /// </summary>
    [Required]
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the payment amount
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the payment method
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the idempotency key for duplicate prevention
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 10)]
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional payment data (e.g., card token, bank details)
    /// </summary>
    public Dictionary<string, object>? PaymentData { get; set; }
}

/// <summary>
/// DTO for payment intent
/// </summary>
public class PaymentIntentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentIntentStatus Status { get; set; }
    public string? Provider { get; set; }
    public string? ClientSecret { get; set; }
    public string? CallbackUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// DTO for refund information
/// </summary>
public class RefundDto
{
    /// <summary>
    /// Gets or sets the refund ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the payment ID
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Gets or sets the refund amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the refund reason
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refund status
    /// </summary>
    public RefundStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the transaction ID from payment provider
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the processed date
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the idempotency key
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Gets or sets the error message if refund failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for processing a refund
/// </summary>
public class ProcessRefundDto
{
    /// <summary>
    /// Gets or sets the refund amount
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the refund reason
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the idempotency key for duplicate prevention
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 10)]
    public string IdempotencyKey { get; set; } = string.Empty;
}