using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

public class SubscriptionPayment : EntityBase
{
    public Guid SubscriptionId { get; set; }
    public virtual Subscription Subscription { get; set; } = null!;

    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PaymentProvider { get; set; } = string.Empty;

    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }

    [MaxLength(200)]
    public string? GatewayTransactionId { get; set; }

    [MaxLength(2000)]
    public string? GatewayResponse { get; set; }

    [MaxLength(1000)]
    public string? FailureReason { get; set; }

    public DateTime? ProcessedAt { get; set; }
}