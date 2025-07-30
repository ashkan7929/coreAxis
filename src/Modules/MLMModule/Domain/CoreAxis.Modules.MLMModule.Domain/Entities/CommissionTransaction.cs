using CoreAxis.SharedKernel;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using CoreAxis.Modules.MLMModule.Domain.Events;

namespace CoreAxis.Modules.MLMModule.Domain.Entities;

public class CommissionTransaction : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid SourcePaymentId { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid CommissionRuleSetId { get; private set; }
    public int Level { get; private set; }
    public decimal Amount { get; private set; }
    public decimal SourceAmount { get; private set; }
    public decimal Percentage { get; private set; }
    public CommissionStatus Status { get; private set; } = CommissionStatus.Pending;
    public bool IsSettled { get; private set; } = false;
    public Guid? WalletTransactionId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    
    // Navigation properties
    public virtual UserReferral UserReferral { get; private set; } = null!;
    public virtual CommissionRuleSet CommissionRuleSet { get; private set; } = null!;
    
    private CommissionTransaction() { } // For EF Core
    
    public CommissionTransaction(
        Guid userId,
        Guid sourcePaymentId,
        Guid commissionRuleSetId,
        int level,
        decimal amount,
        decimal sourceAmount,
        decimal percentage,
        Guid? productId = null)
    {
        UserId = userId;
        SourcePaymentId = sourcePaymentId;
        CommissionRuleSetId = commissionRuleSetId;
        Level = level;
        Amount = amount;
        SourceAmount = sourceAmount;
        Percentage = percentage;
        ProductId = productId;
        CreatedOn = DateTime.UtcNow;
        
        ValidateAmount();
        ValidateLevel();
        
        AddDomainEvent(new CommissionGeneratedEvent(Id, userId, amount, level, sourcePaymentId));
    }
    
    public void Approve(string? notes = null)
    {
        if (Status != CommissionStatus.Pending)
            throw new InvalidOperationException($"Cannot approve commission in {Status} status");
            
        Status = CommissionStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        Notes = notes;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new CommissionApprovedEvent(Id, UserId, Amount));
    }
    
    public void Reject(string rejectionReason)
    {
        if (Status != CommissionStatus.Pending)
            throw new InvalidOperationException($"Cannot reject commission in {Status} status");
            
        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Rejection reason is required", nameof(rejectionReason));
            
        Status = CommissionStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = rejectionReason;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new CommissionRejectedEvent(Id, UserId, Amount, rejectionReason));
    }
    
    public void MarkAsPaid(Guid walletTransactionId)
    {
        if (Status != CommissionStatus.Approved)
            throw new InvalidOperationException($"Cannot mark commission as paid in {Status} status");
            
        Status = CommissionStatus.Paid;
        IsSettled = true;
        WalletTransactionId = walletTransactionId;
        PaidAt = DateTime.UtcNow;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new CommissionPaidEvent(Id, UserId, Amount, walletTransactionId));
    }
    
    public void MarkAsExpired()
    {
        if (Status != CommissionStatus.Pending)
            throw new InvalidOperationException($"Cannot mark commission as expired in {Status} status");
            
        Status = CommissionStatus.Expired;
        LastModifiedOn = DateTime.UtcNow;
        
        AddDomainEvent(new CommissionExpiredEvent(Id, UserId, Amount));
    }
    
    public void UpdateNotes(string notes)
    {
        Notes = notes;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    private void ValidateAmount()
    {
        if (Amount <= 0)
            throw new ArgumentException("Commission amount must be positive", nameof(Amount));
            
        if (SourceAmount <= 0)
            throw new ArgumentException("Source amount must be positive", nameof(SourceAmount));
    }
    
    private void ValidateLevel()
    {
        if (Level < 1)
            throw new ArgumentException("Level must be greater than 0", nameof(Level));
    }
}