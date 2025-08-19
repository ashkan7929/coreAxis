using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Queries;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.SharedKernel.Exceptions;
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

public class GetDefaultCommissionRuleSetQueryHandler : IRequestHandler<GetDefaultCommissionRuleSetQuery, CommissionRuleSetDto?>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;

    public GetDefaultCommissionRuleSetQueryHandler(ICommissionRuleSetRepository ruleSetRepository)
    {
        _ruleSetRepository = ruleSetRepository;
    }

    public async Task<CommissionRuleSetDto?> Handle(GetDefaultCommissionRuleSetQuery request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetDefaultAsync();
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
        var ruleSet = await _ruleSetRepository.GetByProductIdAsync(request.ProductId);
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
        var ruleSets = await _ruleSetRepository.GetActiveAsync();
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
        var ruleSets = await _ruleSetRepository.GetAllAsync();
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
        if (ruleSet == null)
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
        // Get all rule sets and filter by product bindings
        var ruleSets = await _ruleSetRepository.GetAllAsync();
        
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

public class GetCommissionRuleVersionsQueryHandler : IRequestHandler<GetCommissionRuleVersionsQuery, IEnumerable<CommissionRuleVersionDto>>
{
    private readonly ICommissionRuleSetRepository _repository;

    public GetCommissionRuleVersionsQueryHandler(ICommissionRuleSetRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CommissionRuleVersionDto>> Handle(GetCommissionRuleVersionsQuery query, CancellationToken cancellationToken)
    {
        var ruleSet = await _repository.GetByIdAsync(query.RuleSetId);
        if (ruleSet == null)
        {
            throw new EntityNotFoundException("CommissionRuleSet", query.RuleSetId);
        }

        return ruleSet.Versions.Select(MapToVersionDto).ToList();
    }

    private static CommissionRuleVersionDto MapToVersionDto(CommissionRuleVersion version)
    {
        return new CommissionRuleVersionDto
        {
            Id = version.Id,
            RuleSetId = version.RuleSetId,
            Version = version.Version,
            SchemaJson = version.SchemaJson,
            IsPublished = version.IsPublished,
            PublishedAt = version.PublishedAt,
            PublishedBy = version.PublishedBy,
            CreatedOn = version.CreatedOn
        };
    }
}

public class GetCommissionRuleVersionByIdQueryHandler : IRequestHandler<GetCommissionRuleVersionByIdQuery, CommissionRuleVersionDto?>
{
    private readonly ICommissionRuleSetRepository _repository;

    public GetCommissionRuleVersionByIdQueryHandler(ICommissionRuleSetRepository repository)
    {
        _repository = repository;
    }

    public async Task<CommissionRuleVersionDto?> Handle(GetCommissionRuleVersionByIdQuery query, CancellationToken cancellationToken)
    {
        // First, we need to find the version by its ID across all rule sets
        // We'll get all rule sets in batches to find the version
        var allRuleSets = await _repository.GetAllAsync(0, 1000, cancellationToken);
        
        CommissionRuleVersion? version = null;
        foreach (var ruleSet in allRuleSets)
        {
            version = ruleSet.Versions.FirstOrDefault(v => v.Id == query.VersionId);
            if (version != null)
                break;
        }

        if (version == null)
        {
            throw new EntityNotFoundException("CommissionRuleVersion", query.VersionId);
        }

        return new CommissionRuleVersionDto
        {
            Id = version.Id,
            RuleSetId = version.RuleSetId,
            Version = version.Version,
            SchemaJson = version.SchemaJson,
            IsPublished = version.IsPublished,
            PublishedAt = version.PublishedAt,
            PublishedBy = version.PublishedBy,
            CreatedOn = version.CreatedOn
        };
    }
}

public class GetLatestCommissionRuleVersionQueryHandler : IRequestHandler<GetLatestCommissionRuleVersionQuery, CommissionRuleVersionDto?>
{
    private readonly ICommissionRuleSetRepository _repository;

    public GetLatestCommissionRuleVersionQueryHandler(ICommissionRuleSetRepository repository)
    {
        _repository = repository;
    }

    public async Task<CommissionRuleVersionDto?> Handle(GetLatestCommissionRuleVersionQuery query, CancellationToken cancellationToken)
    {
        var ruleSet = await _repository.GetByIdAsync(query.RuleSetId);
        if (ruleSet == null)
        {
            throw new EntityNotFoundException("CommissionRuleSet", query.RuleSetId);
        }

        var latestVersion = ruleSet.Versions
            .Where(v => v.IsActive)
            .OrderByDescending(v => v.Version)
            .FirstOrDefault();

        if (latestVersion == null)
        {
            throw new EntityNotFoundException("CommissionRuleVersion", $"No active version for RuleSet {query.RuleSetId}");
        }

        return new CommissionRuleVersionDto
        {
            Id = latestVersion.Id,
            RuleSetId = latestVersion.RuleSetId,
            Version = latestVersion.Version,
            SchemaJson = latestVersion.SchemaJson,
            IsPublished = latestVersion.IsPublished,
            PublishedAt = latestVersion.PublishedAt,
            PublishedBy = latestVersion.PublishedBy,
            CreatedOn = latestVersion.CreatedOn
        };
    }
}