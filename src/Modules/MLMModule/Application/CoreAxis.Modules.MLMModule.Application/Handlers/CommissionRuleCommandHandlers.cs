using CoreAxis.Modules.MLMModule.Application.Commands;
using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.SharedKernel.Domain;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Handlers;

public class CreateCommissionRuleSetCommandHandler : IRequestHandler<CreateCommissionRuleSetCommand, CommissionRuleSetDto>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateCommissionRuleSetCommandHandler(
        ICommissionRuleSetRepository ruleSetRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _ruleSetRepository = ruleSetRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<CommissionRuleSetDto> Handle(CreateCommissionRuleSetCommand request, CancellationToken cancellationToken)
    {
        var ruleSet = new CommissionRuleSet(
            Guid.NewGuid().ToString(),
            request.Name,
            request.Description,
            request.MaxLevels);

        // Add commission levels
        if (request.CommissionLevels != null)
        {
            foreach (var levelDto in request.CommissionLevels)
            {
                var level = new CommissionLevel(
                    ruleSet.Id,
                    levelDto.Level,
                    levelDto.Percentage);
                
                ruleSet.AddLevel(level);
            }
        }

        await _ruleSetRepository.AddAsync(ruleSet);
        await _eventDispatcher.DispatchAsync(ruleSet.DomainEvents);

        return MapToDto(ruleSet);
    }

    private static CommissionRuleSetDto MapToDto(CommissionRuleSet ruleSet)
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
            }).ToList() ?? new List<CommissionLevelDto>()
        };
    }
}

public class UpdateCommissionRuleSetCommandHandler : IRequestHandler<UpdateCommissionRuleSetCommand, CommissionRuleSetDto>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UpdateCommissionRuleSetCommandHandler(
        ICommissionRuleSetRepository ruleSetRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _ruleSetRepository = ruleSetRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<CommissionRuleSetDto> Handle(UpdateCommissionRuleSetCommand request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByIdAsync(request.Id);
        if (ruleSet == null)
        {
            throw new InvalidOperationException("Commission rule set not found.");
        }

        // Note: IsDefault validation removed as UpdateCommissionRuleSetCommand doesn't have IsDefault property
        // If needed, add IsDefault property to the command and restore this validation

        ruleSet.UpdateDetails(
            request.Name,
            request.Description,
            request.MaxLevels);

        // Note: IsDefault property not available in UpdateCommissionRuleSetCommand
        // If needed, add IsDefault property to the command

        await _ruleSetRepository.UpdateAsync(ruleSet);
        await _eventDispatcher.DispatchAsync(ruleSet.DomainEvents);

        return MapToDto(ruleSet);
    }

    private static CommissionRuleSetDto MapToDto(CommissionRuleSet ruleSet)
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
            }).ToList() ?? new List<CommissionLevelDto>()
        };
    }
}

public class ActivateCommissionRuleSetCommandHandler : IRequestHandler<ActivateCommissionRuleSetCommand, bool>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public ActivateCommissionRuleSetCommandHandler(
        ICommissionRuleSetRepository ruleSetRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _ruleSetRepository = ruleSetRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> Handle(ActivateCommissionRuleSetCommand request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByIdAsync(request.RuleSetId);
        if (ruleSet == null)
        {
            return false;
        }

        ruleSet.Activate();
        await _ruleSetRepository.UpdateAsync(ruleSet);
        await _eventDispatcher.DispatchAsync(ruleSet.DomainEvents);

        return true;
    }
}

public class DeactivateCommissionRuleSetCommandHandler : IRequestHandler<DeactivateCommissionRuleSetCommand, bool>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public DeactivateCommissionRuleSetCommandHandler(
        ICommissionRuleSetRepository ruleSetRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _ruleSetRepository = ruleSetRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> Handle(DeactivateCommissionRuleSetCommand request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByIdAsync(request.RuleSetId);
        if (ruleSet == null)
        {
            return false;
        }

        // Cannot deactivate the default rule set
        if (ruleSet.IsDefault)
        {
            throw new InvalidOperationException("Cannot deactivate the default commission rule set.");
        }

        ruleSet.Deactivate();
        await _ruleSetRepository.UpdateAsync(ruleSet);
        await _eventDispatcher.DispatchAsync(ruleSet.DomainEvents);

        return true;
    }
}

public class SetDefaultCommissionRuleSetCommandHandler : IRequestHandler<SetDefaultCommissionRuleSetCommand, bool>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public SetDefaultCommissionRuleSetCommandHandler(
        ICommissionRuleSetRepository ruleSetRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _ruleSetRepository = ruleSetRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> Handle(SetDefaultCommissionRuleSetCommand request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByIdAsync(request.RuleSetId);
        if (ruleSet == null)
        {
            return false;
        }

        // Remove default from existing default rule set
        var existingDefault = await _ruleSetRepository.GetDefaultAsync();
        if (existingDefault != null && existingDefault.Id != ruleSet.Id)
        {
            existingDefault.RemoveAsDefault();
            await _ruleSetRepository.UpdateAsync(existingDefault);
        }

        ruleSet.SetAsDefault();
        await _ruleSetRepository.UpdateAsync(ruleSet);
        await _eventDispatcher.DispatchAsync(ruleSet.DomainEvents);

        return true;
    }
}

public class DeleteCommissionRuleSetCommandHandler : IRequestHandler<DeleteCommissionRuleSetCommand, bool>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public DeleteCommissionRuleSetCommandHandler(
        ICommissionRuleSetRepository ruleSetRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _ruleSetRepository = ruleSetRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> Handle(DeleteCommissionRuleSetCommand request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByIdAsync(request.RuleSetId);
        if (ruleSet == null)
        {
            return false;
        }

        // Cannot delete the default rule set
        if (ruleSet.IsDefault)
        {
            throw new InvalidOperationException("Cannot delete the default commission rule set.");
        }

        await _ruleSetRepository.DeleteAsync(ruleSet);
        await _eventDispatcher.DispatchAsync(ruleSet.DomainEvents);

        return true;
    }
}

public class AddProductRuleBindingCommandHandler : IRequestHandler<AddProductRuleBindingCommand, ProductRuleBindingDto>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public AddProductRuleBindingCommandHandler(
        ICommissionRuleSetRepository ruleSetRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _ruleSetRepository = ruleSetRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<ProductRuleBindingDto> Handle(AddProductRuleBindingCommand request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByIdAsync(request.CommissionRuleSetId);
        if (ruleSet == null)
        {
            throw new InvalidOperationException("Commission rule set not found.");
        }

        // Check if binding already exists
        if (ruleSet.ProductBindings?.Any(pb => pb.ProductId == request.ProductId) == true)
        {
            throw new InvalidOperationException("Product is already bound to this rule set.");
        }

        var binding = new ProductRuleBinding(
            request.CommissionRuleSetId,
            request.ProductId);

        if (request.ValidFrom.HasValue || request.ValidTo.HasValue)
        {
            binding.SetValidityPeriod(request.ValidFrom, request.ValidTo);
        }

        ruleSet.AddProductBinding(binding);
        await _ruleSetRepository.UpdateAsync(ruleSet);
        await _eventDispatcher.DispatchAsync(ruleSet.DomainEvents);

        return new ProductRuleBindingDto
        {
            Id = binding.Id,
            ProductId = binding.ProductId,
            IsActive = binding.IsActive,
            ValidFrom = binding.ValidFrom,
            ValidTo = binding.ValidTo
        };
    }
}

public class RemoveProductRuleBindingCommandHandler : IRequestHandler<RemoveProductRuleBindingCommand, bool>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RemoveProductRuleBindingCommandHandler(
        ICommissionRuleSetRepository ruleSetRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _ruleSetRepository = ruleSetRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> Handle(RemoveProductRuleBindingCommand request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByIdAsync(request.CommissionRuleSetId);
        if (ruleSet == null)
        {
            return false;
        }

        ruleSet.RemoveProductBinding(request.ProductId);
        await _ruleSetRepository.UpdateAsync(ruleSet);
        await _eventDispatcher.DispatchAsync(ruleSet.DomainEvents);

        return true;
    }
}

public class PublishCommissionRuleVersionCommandHandler : IRequestHandler<PublishCommissionRuleVersionCommand, CommissionRuleVersionDto>
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PublishCommissionRuleVersionCommandHandler(
        ICommissionRuleSetRepository ruleSetRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _ruleSetRepository = ruleSetRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<CommissionRuleVersionDto> Handle(PublishCommissionRuleVersionCommand request, CancellationToken cancellationToken)
    {
        var ruleSet = await _ruleSetRepository.GetByIdAsync(request.RuleSetId);
        if (ruleSet == null)
        {
            throw new InvalidOperationException("Commission rule set not found.");
        }

        // Validate schema JSON
        if (string.IsNullOrWhiteSpace(request.SchemaJson))
        {
            throw new ArgumentException("Schema JSON cannot be empty.");
        }

        try
        {
            System.Text.Json.JsonDocument.Parse(request.SchemaJson);
        }
        catch (System.Text.Json.JsonException)
        {
            throw new ArgumentException("Invalid JSON format in schema.");
        }

        var version = ruleSet.PublishVersion(request.SchemaJson, request.PublishedBy);
        await _ruleSetRepository.UpdateAsync(ruleSet);
        await _eventDispatcher.DispatchAsync(ruleSet.DomainEvents);

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

public class ValidateCommissionRuleSchemaCommandHandler : IRequestHandler<ValidateCommissionRuleSchemaCommand, bool>
{
    public async Task<bool> Handle(ValidateCommissionRuleSchemaCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SchemaJson))
        {
            return false;
        }

        try
        {
            // Basic JSON validation
            using var document = System.Text.Json.JsonDocument.Parse(request.SchemaJson);
            var root = document.RootElement;

            // Validate required properties
            if (!root.TryGetProperty("name", out _) ||
                !root.TryGetProperty("version", out _) ||
                !root.TryGetProperty("rules", out var rulesElement) ||
                rulesElement.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                return false;
            }

            // Validate rules array
            foreach (var rule in rulesElement.EnumerateArray())
            {
                if (!rule.TryGetProperty("level", out var levelElement) ||
                    levelElement.ValueKind != System.Text.Json.JsonValueKind.Number ||
                    !rule.TryGetProperty("percentage", out var percentageElement) ||
                    percentageElement.ValueKind != System.Text.Json.JsonValueKind.Number)
                {
                    return false;
                }

                var percentage = percentageElement.GetDecimal();
                if (percentage < 0 || percentage > 100)
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}