using CoreAxis.Modules.MLMModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Queries;

public class GetCommissionRuleSetByIdQuery : IRequest<CommissionRuleSetDto?>
{
    public Guid RuleSetId { get; set; }
}

public class GetDefaultCommissionRuleSetQuery : IRequest<CommissionRuleSetDto?>
{
}

public class GetCommissionRuleSetByProductQuery : IRequest<CommissionRuleSetDto?>
{
    public Guid ProductId { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

public class GetActiveCommissionRuleSetsQuery : IRequest<IEnumerable<CommissionRuleSetDto>>
{
}

public class GetAllCommissionRuleSetsQuery : IRequest<IEnumerable<CommissionRuleSetDto>>
{
    public bool ActiveOnly { get; set; } = false;
}

public class GetProductRuleBindingsQuery : IRequest<IEnumerable<ProductRuleBindingDto>>
{
    public Guid RuleSetId { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public class GetProductRuleBindingsByProductQuery : IRequest<IEnumerable<ProductRuleBindingDto>>
{
    public Guid ProductId { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

public class GetCommissionRuleVersionsQuery : IRequest<IEnumerable<CommissionRuleVersionDto>>
{
    public Guid RuleSetId { get; set; }
    public bool PublishedOnly { get; set; } = true;
}

public class GetCommissionRuleVersionByIdQuery : IRequest<CommissionRuleVersionDto?>
{
    public Guid VersionId { get; set; }
}

public class GetLatestCommissionRuleVersionQuery : IRequest<CommissionRuleVersionDto?>
{
    public Guid RuleSetId { get; set; }
}