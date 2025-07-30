using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.MLMModule.Domain.Entities;

public class ProductRuleBinding : EntityBase
{
    public Guid CommissionRuleSetId { get; private set; }
    public Guid ProductId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    
    // Navigation properties
    public virtual CommissionRuleSet CommissionRuleSet { get; private set; } = null!;
    
    private ProductRuleBinding() { } // For EF Core
    
    public ProductRuleBinding(Guid commissionRuleSetId, Guid productId)
    {
        CommissionRuleSetId = commissionRuleSetId;
        ProductId = productId;
        CreatedOn = DateTime.UtcNow;
    }
    
    public void SetValidityPeriod(DateTime? validFrom, DateTime? validTo)
    {
        if (validFrom.HasValue && validTo.HasValue && validFrom.Value >= validTo.Value)
            throw new ArgumentException("Valid from date must be before valid to date");
            
        ValidFrom = validFrom;
        ValidTo = validTo;
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
    
    public bool IsValidForDate(DateTime date)
    {
        if (!IsActive)
            return false;
            
        if (ValidFrom.HasValue && date < ValidFrom.Value)
            return false;
            
        if (ValidTo.HasValue && date > ValidTo.Value)
            return false;
            
        return true;
    }
}