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