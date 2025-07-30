using CoreAxis.Modules.MLMModule.Application.Commands;
using FluentValidation;

namespace CoreAxis.Modules.MLMModule.Application.Validators;

public class CreateCommissionRuleSetCommandValidator : AbstractValidator<CreateCommissionRuleSetCommand>
{
    public CreateCommissionRuleSetCommandValidator()
    {


        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Rule set name is required.")
            .MaximumLength(100)
            .WithMessage("Rule set name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.MaxLevels)
            .GreaterThan(0)
            .WithMessage("Maximum levels must be greater than 0.")
            .LessThanOrEqualTo(20)
            .WithMessage("Maximum levels cannot exceed 20.");

        RuleForEach(x => x.CommissionLevels)
            .SetValidator(new CreateCommissionLevelDtoValidator())
            .When(x => x.CommissionLevels != null);

        RuleFor(x => x.CommissionLevels)
            .Must(levels => levels == null || levels.Select(l => l.Level).Distinct().Count() == levels.Count())
            .WithMessage("Commission levels must be unique.");

        RuleFor(x => x.CommissionLevels)
            .Must((command, levels) => levels == null || levels.All(l => l.Level <= command.MaxLevels))
            .WithMessage("Commission levels cannot exceed the maximum levels setting.");
    }
}

public class CreateCommissionLevelDtoValidator : AbstractValidator<Application.DTOs.CreateCommissionLevelDto>
{
    public CreateCommissionLevelDtoValidator()
    {
        RuleFor(x => x.Level)
            .GreaterThan(0)
            .WithMessage("Level must be greater than 0.")
            .LessThanOrEqualTo(20)
            .WithMessage("Level cannot exceed 20.");

        RuleFor(x => x.Percentage)
            .GreaterThan(0)
            .WithMessage("Percentage must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("Percentage cannot exceed 100%.");

        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinAmount.HasValue)
            .WithMessage("Minimum amount must be greater than or equal to 0.");

        RuleFor(x => x.MaxAmount)
            .GreaterThan(0)
            .When(x => x.MaxAmount.HasValue)
            .WithMessage("Maximum amount must be greater than 0.");
            
        RuleFor(x => x.FixedAmount)
            .GreaterThan(0)
            .When(x => x.FixedAmount.HasValue)
            .WithMessage("Fixed amount must be greater than 0.");
    }
}

public class UpdateCommissionRuleSetCommandValidator : AbstractValidator<UpdateCommissionRuleSetCommand>
{
    public UpdateCommissionRuleSetCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Rule set ID is required.");



        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Rule set name is required.")
            .MaximumLength(100)
            .WithMessage("Rule set name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.MaxLevels)
            .GreaterThan(0)
            .WithMessage("Maximum levels must be greater than 0.")
            .LessThanOrEqualTo(20)
            .WithMessage("Maximum levels cannot exceed 20.");
    }
}

public class ActivateCommissionRuleSetCommandValidator : AbstractValidator<ActivateCommissionRuleSetCommand>
{
    public ActivateCommissionRuleSetCommandValidator()
    {
        RuleFor(x => x.RuleSetId)
            .NotEmpty()
            .WithMessage("Rule set ID is required.");


    }
}

public class DeactivateCommissionRuleSetCommandValidator : AbstractValidator<DeactivateCommissionRuleSetCommand>
{
    public DeactivateCommissionRuleSetCommandValidator()
    {
        RuleFor(x => x.RuleSetId)
            .NotEmpty()
            .WithMessage("Rule set ID is required.");


    }
}

public class SetDefaultCommissionRuleSetCommandValidator : AbstractValidator<SetDefaultCommissionRuleSetCommand>
{
    public SetDefaultCommissionRuleSetCommandValidator()
    {
        RuleFor(x => x.RuleSetId)
            .NotEmpty()
            .WithMessage("Rule set ID is required.");


    }
}

public class DeleteCommissionRuleSetCommandValidator : AbstractValidator<DeleteCommissionRuleSetCommand>
{
    public DeleteCommissionRuleSetCommandValidator()
    {
        RuleFor(x => x.RuleSetId)
            .NotEmpty()
            .WithMessage("Rule set ID is required.");


    }
}

public class AddProductRuleBindingCommandValidator : AbstractValidator<AddProductRuleBindingCommand>
{
    public AddProductRuleBindingCommandValidator()
    {
        RuleFor(x => x.CommissionRuleSetId)
            .NotEmpty()
            .WithMessage("Commission rule set ID is required.");



        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.ValidFrom)
            .LessThan(x => x.ValidTo)
            .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue)
            .WithMessage("Valid from date must be before valid to date.");
    }
}