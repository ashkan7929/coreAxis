using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.MLMModule.Domain.Entities;

public class CommissionRuleSet : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsDefault { get; private set; } = false;
    public int MaxLevels { get; private set; } = 10;
    public decimal MinimumPurchaseAmount { get; private set; } = 0;
    public bool RequireActiveUpline { get; private set; } = true;
    
    // Navigation properties
    public virtual ICollection<CommissionLevel> CommissionLevels { get; private set; } = new List<CommissionLevel>();
    public virtual ICollection<ProductRuleBinding> ProductBindings { get; private set; } = new List<ProductRuleBinding>();
    
    private CommissionRuleSet() { } // For EF Core
    
    public CommissionRuleSet(string name, string description, int maxLevels = 10)
    {
        Name = name;
        Description = description;
        MaxLevels = maxLevels;
        CreatedOn = DateTime.UtcNow;
    }
    
    public void UpdateDetails(string name, string description, int maxLevels)
    {
        Name = name;
        Description = description;
        MaxLevels = maxLevels;
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
    
    public void SetAsDefault()
    {
        IsDefault = true;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void RemoveAsDefault()
    {
        IsDefault = false;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void SetMinimumPurchaseAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Minimum purchase amount cannot be negative", nameof(amount));
            
        MinimumPurchaseAmount = amount;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void SetRequireActiveUpline(bool require)
    {
        RequireActiveUpline = require;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void AddProductBinding(ProductRuleBinding binding)
    {
        if (binding == null)
            throw new ArgumentNullException(nameof(binding));
            
        if (ProductBindings.Any(pb => pb.ProductId == binding.ProductId))
            throw new InvalidOperationException("Product is already bound to this rule set.");
            
        ((List<ProductRuleBinding>)ProductBindings).Add(binding);
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void RemoveProductBinding(Guid productId)
    {
        var binding = ProductBindings.FirstOrDefault(pb => pb.ProductId == productId);
        if (binding != null)
        {
            ((List<ProductRuleBinding>)ProductBindings).Remove(binding);
            LastModifiedOn = DateTime.UtcNow;
        }
    }
    
    public void AddLevel(CommissionLevel level)
    {
        if (level == null)
            throw new ArgumentNullException(nameof(level));
            
        // Check if level already exists
        if (CommissionLevels.Any(l => l.Level == level.Level))
            throw new InvalidOperationException($"Commission level {level.Level} already exists in this rule set.");
            
        // Validate level doesn't exceed max levels
        if (level.Level > MaxLevels)
            throw new InvalidOperationException($"Commission level {level.Level} exceeds maximum allowed levels ({MaxLevels}).");
            
        CommissionLevels.Add(level);
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void RemoveLevel(CommissionLevel level)
    {
        if (level == null)
            throw new ArgumentNullException(nameof(level));
            
        CommissionLevels.Remove(level);
        LastModifiedOn = DateTime.UtcNow;
    }
}