using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Queries;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Handlers;

public class GetCommissionRuleSetByIdQueryHandler : IRequestHandler<GetCommissionRuleSetByIdQuery, CommissionRuleSetDto?>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;

    public GetCommissionRuleSetByIdQueryHandler(ICommissionRuleSetRepository ruleSetRepository)
    {
        _ruleSetRepository = ruleSetRepository;
    }

    public async Task<CommissionRuleSetDto?> Handle(GetCommissionRuleSetByIdQuery request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByIdAsync(request.RuleSetId);
        if (ruleSet == null || ruleSet.TenantId != request.TenantId)
        {
            return null;
        }

        return MapToDto(ruleSet);
    }

    private static CommissionRuleSetDto MapToDto(Domain.Entities.CommissionRuleSet ruleSet)
    {
        return new CommissionRuleSetDto
        {
            Id = ruleSet.Id,
            Name = ruleSet.Name,
            Description = ruleSet.Description,
            IsActive = ruleSet.IsActive,
            IsDefault = ruleSet.IsDefault,
            MaxLevels = ruleSet.MaxLevels,
            CreatedOn = ruleSet.CreatedOn,
            CommissionLevels = ruleSet.CommissionLevels?.Select(l => new CommissionLevelDto
            {
                Id = l.Id,
                Level = l.Level,
                Percentage = l.Percentage,
                IsActive = l.IsActive
            }).ToList() ?? new List<CommissionLevelDto>(),
            ProductBindings = ruleSet.ProductBindings?.Select(pb => new ProductRuleBindingDto
            {
                Id = pb.Id,
                ProductId = pb.ProductId,
                IsActive = pb.IsActive
            }).ToList() ?? new List<ProductRuleBindingDto>()
        };
    }
}

public class GetDefaultCommissionRuleSetQueryHandler : IRequestHandler<GetDefaultCommissionRuleSetQuery, CommissionRuleSetDto?>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;

    public GetDefaultCommissionRuleSetQueryHandler(ICommissionRuleSetRepository ruleSetRepository)
    {
        _ruleSetRepository = ruleSetRepository;
    }

    public async Task<CommissionRuleSetDto?> Handle(GetDefaultCommissionRuleSetQuery request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetDefaultAsync(request.TenantId);
        if (ruleSet == null)
        {
            return null;
        }

        return MapToDto(ruleSet);
    }

    private static CommissionRuleSetDto MapToDto(Domain.Entities.CommissionRuleSet ruleSet)
    {
        return new CommissionRuleSetDto
        {
            Id = ruleSet.Id,
            Name = ruleSet.Name,
            Description = ruleSet.Description,
            IsActive = ruleSet.IsActive,
            IsDefault = ruleSet.IsDefault,
            MaxLevels = ruleSet.MaxLevels,
            CreatedOn = ruleSet.CreatedOn,
            CommissionLevels = ruleSet.CommissionLevels?.Select(l => new CommissionLevelDto
            {
                Id = l.Id,
                Level = l.Level,
                Percentage = l.Percentage,
                IsActive = l.IsActive
            }).ToList() ?? new List<CommissionLevelDto>(),
            ProductBindings = ruleSet.ProductBindings?.Select(pb => new ProductRuleBindingDto
            {
                Id = pb.Id,
                ProductId = pb.ProductId,
                IsActive = pb.IsActive
            }).ToList() ?? new List<ProductRuleBindingDto>()
        };
    }
}

public class GetCommissionRuleSetByProductQueryHandler : IRequestHandler<GetCommissionRuleSetByProductQuery, CommissionRuleSetDto?>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;

    public GetCommissionRuleSetByProductQueryHandler(ICommissionRuleSetRepository ruleSetRepository)
    {
        _ruleSetRepository = ruleSetRepository;
    }

    public async Task<CommissionRuleSetDto?> Handle(GetCommissionRuleSetByProductQuery request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByProductIdAsync(request.ProductId, request.TenantId);
        if (ruleSet == null)
        {
            return null;
        }

        return MapToDto(ruleSet);
    }

    private static CommissionRuleSetDto MapToDto(Domain.Entities.CommissionRuleSet ruleSet)
    {
        return new CommissionRuleSetDto
        {
            Id = ruleSet.Id,
            Name = ruleSet.Name,
            Description = ruleSet.Description,
            IsActive = ruleSet.IsActive,
            IsDefault = ruleSet.IsDefault,
            MaxLevels = ruleSet.MaxLevels,
            CreatedOn = ruleSet.CreatedOn,
            CommissionLevels = ruleSet.CommissionLevels?.Select(l => new CommissionLevelDto
            {
                Id = l.Id,
                Level = l.Level,
                Percentage = l.Percentage,
                IsActive = l.IsActive
            }).ToList() ?? new List<CommissionLevelDto>(),
            ProductBindings = ruleSet.ProductBindings?.Select(pb => new ProductRuleBindingDto
            {
                Id = pb.Id,
                ProductId = pb.ProductId,
                IsActive = pb.IsActive
            }).ToList() ?? new List<ProductRuleBindingDto>()
        };
    }
}

public class GetActiveCommissionRuleSetsQueryHandler : IRequestHandler<GetActiveCommissionRuleSetsQuery, IEnumerable<CommissionRuleSetDto>>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;

    public GetActiveCommissionRuleSetsQueryHandler(ICommissionRuleSetRepository ruleSetRepository)
    {
        _ruleSetRepository = ruleSetRepository;
    }

    public async Task<IEnumerable<CommissionRuleSetDto>> Handle(GetActiveCommissionRuleSetsQuery request, CancellationToken cancellationToken)
    {
        var ruleSets = await _ruleSetRepository.GetActiveAsync(request.TenantId);
        return ruleSets.Select(MapToDto);
    }

    private static CommissionRuleSetDto MapToDto(Domain.Entities.CommissionRuleSet ruleSet)
    {
        return new CommissionRuleSetDto
        {
            Id = ruleSet.Id,
            Name = ruleSet.Name,
            Description = ruleSet.Description,
            IsActive = ruleSet.IsActive,
            IsDefault = ruleSet.IsDefault,
            MaxLevels = ruleSet.MaxLevels,
            CreatedOn = ruleSet.CreatedOn,
            CommissionLevels = ruleSet.CommissionLevels?.Select(l => new CommissionLevelDto
            {
                Id = l.Id,
                Level = l.Level,
                Percentage = l.Percentage,
                IsActive = l.IsActive
            }).ToList() ?? new List<CommissionLevelDto>(),
            ProductBindings = ruleSet.ProductBindings?.Select(pb => new ProductRuleBindingDto
            {
                Id = pb.Id,
                ProductId = pb.ProductId,
                IsActive = pb.IsActive
            }).ToList() ?? new List<ProductRuleBindingDto>()
        };
    }
}

public class GetAllCommissionRuleSetsQueryHandler : IRequestHandler<GetAllCommissionRuleSetsQuery, IEnumerable<CommissionRuleSetDto>>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;

    public GetAllCommissionRuleSetsQueryHandler(ICommissionRuleSetRepository ruleSetRepository)
    {
        _ruleSetRepository = ruleSetRepository;
    }

    public async Task<IEnumerable<CommissionRuleSetDto>> Handle(GetAllCommissionRuleSetsQuery request, CancellationToken cancellationToken)
    {
        var ruleSets = await _ruleSetRepository.GetByTenantIdAsync(request.TenantId);
        return ruleSets.Select(MapToDto);
    }

    private static CommissionRuleSetDto MapToDto(Domain.Entities.CommissionRuleSet ruleSet)
    {
        return new CommissionRuleSetDto
        {
            Id = ruleSet.Id,
            Name = ruleSet.Name,
            Description = ruleSet.Description,
            IsActive = ruleSet.IsActive,
            IsDefault = ruleSet.IsDefault,
            MaxLevels = ruleSet.MaxLevels,
            CreatedOn = ruleSet.CreatedOn,
            CommissionLevels = ruleSet.CommissionLevels?.Select(l => new CommissionLevelDto
            {
                Id = l.Id,
                Level = l.Level,
                Percentage = l.Percentage,
                IsActive = l.IsActive
            }).ToList() ?? new List<CommissionLevelDto>(),
            ProductBindings = ruleSet.ProductBindings?.Select(pb => new ProductRuleBindingDto
            {
                Id = pb.Id,
                ProductId = pb.ProductId,
                IsActive = pb.IsActive
            }).ToList() ?? new List<ProductRuleBindingDto>()
        };
    }
}

public class GetProductRuleBindingsQueryHandler : IRequestHandler<GetProductRuleBindingsQuery, IEnumerable<ProductRuleBindingDto>>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;

    public GetProductRuleBindingsQueryHandler(ICommissionRuleSetRepository ruleSetRepository)
    {
        _ruleSetRepository = ruleSetRepository;
    }

    public async Task<IEnumerable<ProductRuleBindingDto>> Handle(GetProductRuleBindingsQuery request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByIdAsync(request.RuleSetId);
        if (ruleSet == null || ruleSet.TenantId != request.TenantId)
        {
            return Enumerable.Empty<ProductRuleBindingDto>();
        }

        return ruleSet.ProductBindings?.Select(pb => new ProductRuleBindingDto
        {
            Id = pb.Id,
            ProductId = pb.ProductId,
            IsActive = pb.IsActive
        }) ?? Enumerable.Empty<ProductRuleBindingDto>();
    }
}

public class GetProductRuleBindingsByProductQueryHandler : IRequestHandler<GetProductRuleBindingsByProductQuery, IEnumerable<ProductRuleBindingDto>>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;

    public GetProductRuleBindingsByProductQueryHandler(ICommissionRuleSetRepository ruleSetRepository)
    {
        _ruleSetRepository = ruleSetRepository;
    }

    public async Task<IEnumerable<ProductRuleBindingDto>> Handle(GetProductRuleBindingsByProductQuery request, CancellationToken cancellationToken)
    {
        // Get all rule sets for the tenant and filter by product bindings
        var ruleSets = await _ruleSetRepository.GetByTenantIdAsync(request.TenantId);
        
        var productBindings = new List<ProductRuleBindingDto>();
        
        foreach (var ruleSet in ruleSets)
        {
            if (ruleSet.ProductBindings != null)
            {
                var bindings = ruleSet.ProductBindings
                    .Where(pb => pb.ProductId == request.ProductId)
                    .Select(pb => new ProductRuleBindingDto
                    {
                        Id = pb.Id,
                        ProductId = pb.ProductId,
                        IsActive = pb.IsActive
                    });
                
                productBindings.AddRange(bindings);
            }
        }
        
        return productBindings;
    }
}