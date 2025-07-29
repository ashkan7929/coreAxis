using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.MLMModule.Domain.Entities;

public class CommissionLevel : EntityBase
{
    public Guid CommissionRuleSetId { get; private set; }
    public int Level { get; private set; }
    public decimal Percentage { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal? MaxAmount { get; private set; }
    public decimal? MinAmount { get; private set; }
    public bool IsActive { get; private set; } = true;
    
    // Navigation properties
    public virtual CommissionRuleSet CommissionRuleSet { get; private set; } = null!;
    
    private CommissionLevel() { } // For EF Core
    
    public CommissionLevel(Guid commissionRuleSetId, int level, decimal percentage, Guid tenantId)
    {
        CommissionRuleSetId = commissionRuleSetId;
        Level = level;
        Percentage = percentage;
        TenantId = tenantId;
        CreatedOn = DateTime.UtcNow;
        
        ValidatePercentage();
        ValidateLevel();
    }
    
    public void UpdatePercentage(decimal percentage)
    {
        Percentage = percentage;
        LastModifiedOn = DateTime.UtcNow;
        ValidatePercentage();
    }
    
    public void SetFixedAmount(decimal? fixedAmount)
    {
        if (fixedAmount.HasValue && fixedAmount.Value < 0)
            throw new ArgumentException("Fixed amount cannot be negative", nameof(fixedAmount));
            
        FixedAmount = fixedAmount;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void SetMaxAmount(decimal? maxAmount)
    {
        if (maxAmount.HasValue && maxAmount.Value < 0)
            throw new ArgumentException("Max amount cannot be negative", nameof(maxAmount));
            
        MaxAmount = maxAmount;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void SetMinAmount(decimal? minAmount)
    {
        if (minAmount.HasValue && minAmount.Value < 0)
            throw new ArgumentException("Min amount cannot be negative", nameof(minAmount));
            
        MinAmount = minAmount;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void Activate()
    {
        IsActive = true;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public decimal CalculateCommission(decimal baseAmount)
    {
        if (!IsActive)
            return 0;
            
        decimal commission;
        
        if (FixedAmount.HasValue)
        {
            commission = FixedAmount.Value;
        }
        else
        {
            commission = baseAmount * (Percentage / 100);
        }
        
        // Apply min/max constraints
        if (MinAmount.HasValue && commission < MinAmount.Value)
            commission = MinAmount.Value;
            
        if (MaxAmount.HasValue && commission > MaxAmount.Value)
            commission = MaxAmount.Value;
            
        return commission;
    }
    
    private void ValidatePercentage()
    {
        if (Percentage < 0 || Percentage > 100)
            throw new ArgumentException("Percentage must be between 0 and 100", nameof(Percentage));
    }
    
    private void ValidateLevel()
    {
        if (Level < 1)
            throw new ArgumentException("Level must be greater than 0", nameof(Level));
    }
}