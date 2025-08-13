using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.WalletModule.Domain.Entities;

public class WalletContract : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid WalletId { get; private set; }
    public Guid ProviderId { get; private set; }
    public decimal MaxAmount { get; private set; }
    public decimal DailyLimit { get; private set; }
    public decimal MonthlyLimit { get; private set; }
    public decimal UsedDailyAmount { get; private set; } = 0;
    public decimal UsedMonthlyAmount { get; private set; } = 0;
    public DateTime LastResetDate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Terms { get; private set; }

    // Navigation properties
    public virtual Wallet Wallet { get; private set; } = null!;
    public virtual WalletProvider Provider { get; private set; } = null!;

    private WalletContract() { } // For EF Core

    public WalletContract(
        Guid userId,
        Guid walletId,
        Guid providerId,
        decimal maxAmount,
        decimal dailyLimit,
        decimal monthlyLimit,
        string? terms = null)
    {
        UserId = userId;
        WalletId = walletId;
        ProviderId = providerId;
        MaxAmount = maxAmount;
        DailyLimit = dailyLimit;
        MonthlyLimit = monthlyLimit;
        Terms = terms;
        LastResetDate = DateTime.UtcNow.Date;
        CreatedOn = DateTime.UtcNow;
        CreatedBy = "System";
        LastModifiedBy = "System";
    }

    public void UpdateLimits(decimal maxAmount, decimal dailyLimit, decimal monthlyLimit)
    {
        MaxAmount = maxAmount;
        DailyLimit = dailyLimit;
        MonthlyLimit = monthlyLimit;
        LastModifiedOn = DateTime.UtcNow;
        LastModifiedBy = "System";
    }

    public bool CanProcessAmount(decimal amount)
    {
        if (!IsActive) return false;
        if (amount > MaxAmount) return false;
        
        ResetLimitsIfNeeded();
        
        return (UsedDailyAmount + amount <= DailyLimit) && 
               (UsedMonthlyAmount + amount <= MonthlyLimit);
    }

    public void RecordUsage(decimal amount)
    {
        ResetLimitsIfNeeded();
        
        UsedDailyAmount += amount;
        UsedMonthlyAmount += amount;
        LastModifiedOn = DateTime.UtcNow;
        LastModifiedBy = "System";
    }

    private void ResetLimitsIfNeeded()
    {
        var today = DateTime.UtcNow.Date;
        
        if (LastResetDate.Date < today)
        {
            UsedDailyAmount = 0;
            
            // Reset monthly if it's a new month
            if (LastResetDate.Month != today.Month || LastResetDate.Year != today.Year)
            {
                UsedMonthlyAmount = 0;
            }
            
            LastResetDate = today;
        }
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOn = DateTime.UtcNow;
        LastModifiedBy = "System";
    }

    public void Activate()
    {
        IsActive = true;
        LastModifiedOn = DateTime.UtcNow;
        LastModifiedBy = "System";
    }
}