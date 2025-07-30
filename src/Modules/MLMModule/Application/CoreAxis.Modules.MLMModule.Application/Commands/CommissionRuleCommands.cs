using CoreAxis.Modules.MLMModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Commands;

public class CreateCommissionRuleSetCommand : IRequest<CommissionRuleSetDto>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxLevels { get; set; } = 10;
    public decimal MinimumPurchaseAmount { get; set; } = 0;
    public bool RequireActiveUpline { get; set; } = true;
    public List<CreateCommissionLevelDto> CommissionLevels { get; set; } = new();
}

public class UpdateCommissionRuleSetCommand : IRequest<CommissionRuleSetDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxLevels { get; set; }
    public decimal MinimumPurchaseAmount { get; set; }
    public bool RequireActiveUpline { get; set; }
    public bool IsActive { get; set; }
    public List<CreateCommissionLevelDto> CommissionLevels { get; set; } = new();
}

public class ActivateCommissionRuleSetCommand : IRequest<bool>
{
    public Guid RuleSetId { get; set; }
}

public class DeactivateCommissionRuleSetCommand : IRequest<bool>
{
    public Guid RuleSetId { get; set; }
}

public class SetDefaultCommissionRuleSetCommand : IRequest<bool>
{
    public Guid RuleSetId { get; set; }
}

public class DeleteCommissionRuleSetCommand : IRequest<bool>
{
    public Guid RuleSetId { get; set; }
}

public class AddProductRuleBindingCommand : IRequest<ProductRuleBindingDto>
{
    public Guid CommissionRuleSetId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}