using CoreAxis.SharedKernel;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.Domain.Entities;

/// <summary>
/// Represents an individual reconciliation entry for a transaction.
/// </summary>
public class ReconciliationEntry : EntityBase
{
    public Guid SessionId { get; set; }
    public string StatementTransactionId { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
    public Guid? OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public MatchStatus MatchStatus { get; set; }
    public int MatchConfidence { get; set; }
    public string? MatchingCriteria { get; set; }
    public decimal AmountDifference { get; set; }
    public double TimeDifference { get; set; }
    public string? UnmatchedReason { get; set; }
    public string? Notes { get; set; }
    public DateTime ProcessedAt { get; set; }
    
    // Navigation properties
    public ReconciliationSession Session { get; set; } = null!;
    public Payment? Payment { get; set; }
    public Order? Order { get; set; }
}