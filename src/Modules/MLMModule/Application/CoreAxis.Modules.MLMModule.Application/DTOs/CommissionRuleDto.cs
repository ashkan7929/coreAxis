namespace CoreAxis.Modules.MLMModule.Application.DTOs;

public class CommissionRuleSetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int MaxLevels { get; set; }
    public decimal MinimumPurchaseAmount { get; set; }
    public bool RequireActiveUpline { get; set; }
    public DateTime CreatedOn { get; set; }
    public List<CommissionLevelDto> CommissionLevels { get; set; } = new();
    public List<ProductRuleBindingDto> ProductBindings { get; set; } = new();
}

public class CreateCommissionRuleSetDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxLevels { get; set; } = 10;
    public decimal MinimumPurchaseAmount { get; set; } = 0;
    public bool RequireActiveUpline { get; set; } = true;
    public List<CreateCommissionLevelDto> CommissionLevels { get; set; } = new();
}

public class UpdateCommissionRuleSetDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxLevels { get; set; }
    public decimal MinimumPurchaseAmount { get; set; }
    public bool RequireActiveUpline { get; set; }
    public bool IsActive { get; set; }
    public List<CreateCommissionLevelDto> CommissionLevels { get; set; } = new();
}

public class CommissionLevelDto
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public decimal Percentage { get; set; }
    public decimal? FixedAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? MinAmount { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCommissionLevelDto
{
    public int Level { get; set; }
    public decimal Percentage { get; set; }
    public decimal? FixedAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? MinAmount { get; set; }
}

public class ProductRuleBindingDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class CreateProductRuleBindingDto
{
    public Guid RuleSetId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class CommissionRuleVersionDto
{
    public Guid Id { get; set; }
    public Guid RuleSetId { get; set; }
    public int Version { get; set; }
    public string SchemaJson { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class CreateCommissionRuleVersionDto
{
    public Guid RuleSetId { get; set; }
    public string SchemaJson { get; set; } = string.Empty;
    public string PublishedBy { get; set; } = string.Empty;
}

public class CreateCommissionRuleVersionRequest
{
    public string SchemaJson { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// Alias for backward compatibility
public class CommissionRuleDto : CommissionRuleSetDto
{
}