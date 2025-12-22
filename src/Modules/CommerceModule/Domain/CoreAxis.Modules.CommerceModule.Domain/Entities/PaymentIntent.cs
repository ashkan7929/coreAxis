using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

public class PaymentIntent : EntityBase
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    public PaymentIntentStatus Status { get; set; } = PaymentIntentStatus.Initiated;

    [MaxLength(50)]
    public string? Provider { get; set; }

    [MaxLength(500)]
    public string? CallbackUrl { get; set; }

    [MaxLength(500)]
    public string? ReturnUrl { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [MaxLength(200)]
    public string? ClientSecret { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [MaxLength(100)]
    public string? ExternalId { get; set; }
}
