using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.MLMModule.Domain.Entities;

public class CommissionRuleSet : EntityBase
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int LatestVersion { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;
    public bool IsDefault { get; private set; } = false;
    public int MaxLevels { get; private set; } = 10;
    public decimal MinimumPurchaseAmount { get; private set; } = 0;
    public bool RequireActiveUpline { get; private set; } = true;
    
    // Navigation properties
    public virtual ICollection<CommissionLevel> CommissionLevels { get; private set; } = new List<CommissionLevel>();
    public virtual ICollection<CommissionRuleVersion> Versions { get; private set; } = new List<CommissionRuleVersion>();
    public virtual ICollection<ProductRuleBinding> ProductBindings { get; private set; } = new List<ProductRuleBinding>();
    
    private CommissionRuleSet() { } // For EF Core
    
    public CommissionRuleSet(string code, string name, string description, int maxLevels = 10)
    {
        Code = code;
        Name = name;
        Description = description;
        MaxLevels = maxLevels;
        CreatedOn = DateTime.UtcNow;
        
        ValidateCode();
    }
    
    public void UpdateDetails(string name, string description, int maxLevels)
    {
        Name = name;
        Description = description;
        MaxLevels = maxLevels;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
            
        Name = name;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void UpdateMaxLevels(int maxLevels)
    {
        if (maxLevels <= 0)
            throw new ArgumentException("Max levels must be greater than 0", nameof(maxLevels));
            
        MaxLevels = maxLevels;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void UpdateMinimumPurchaseAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Minimum purchase amount cannot be negative", nameof(amount));
            
        MinimumPurchaseAmount = amount;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void UpdateRequireActiveUpline(bool require)
    {
        RequireActiveUpline = require;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void IncrementVersion()
    {
        LatestVersion++;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    private void ValidateCode()
    {
        if (string.IsNullOrWhiteSpace(Code))
            throw new ArgumentException("Code is required", nameof(Code));
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
    
    public void ClearCommissionLevels()
    {
        ((List<CommissionLevel>)CommissionLevels).Clear();
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void AddCommissionLevel(int level, decimal percentage, decimal? fixedAmount = null, decimal? maxAmount = null, decimal? minAmount = null)
    {
        if (level <= 0)
            throw new ArgumentException("Level must be greater than 0", nameof(level));
            
        if (level > MaxLevels)
            throw new ArgumentException($"Level {level} exceeds maximum allowed levels ({MaxLevels})", nameof(level));
            
        if (percentage < 0 || percentage > 100)
            throw new ArgumentException("Percentage must be between 0 and 100", nameof(percentage));
            
        if (CommissionLevels.Any(l => l.Level == level))
            throw new InvalidOperationException($"Commission level {level} already exists in this rule set.");
            
        var commissionLevel = new CommissionLevel(Id, level, percentage, fixedAmount, maxAmount, minAmount);
        ((List<CommissionLevel>)CommissionLevels).Add(commissionLevel);
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public CommissionRuleVersion PublishVersion(string schemaJson, string publishedBy)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
            throw new ArgumentException("Schema JSON is required", nameof(schemaJson));
            
        if (string.IsNullOrWhiteSpace(publishedBy))
            throw new ArgumentException("Published by is required", nameof(publishedBy));
            
        var nextVersion = LatestVersion + 1;
        var version = new CommissionRuleVersion(Id, nextVersion, schemaJson);
        version.Publish(publishedBy);
        
        ((List<CommissionRuleVersion>)Versions).Add(version);
        LatestVersion = nextVersion;
        LastModifiedOn = DateTime.UtcNow;
        
        return version;
    }
}