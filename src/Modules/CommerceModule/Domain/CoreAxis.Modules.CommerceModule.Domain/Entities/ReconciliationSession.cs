using CoreAxis.SharedKernel;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents a reconciliation session for payment processing.
/// </summary>
public class ReconciliationSession : EntityBase
{
    public string PaymentProvider { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime StartedAt { get; set; }
    public int TotalTransactions { get; set; }
    public int MatchedTransactions { get; set; }
    public int UnmatchedTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MatchedAmount { get; set; }
    public decimal UnmatchedAmount { get; set; }
    public ReconciliationStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }
}