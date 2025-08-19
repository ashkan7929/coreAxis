using CoreAxis.Modules.MLMModule.Application.DTOs;
using FluentValidation;

namespace CoreAxis.Modules.MLMModule.Application.Validators;

public class CreateCommissionRuleSetDtoValidator : AbstractValidator<CreateCommissionRuleSetDto>
{
    public CreateCommissionRuleSetDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.MaxLevels)
            .GreaterThan(0).WithMessage("Max levels must be greater than 0")
            .LessThanOrEqualTo(20).WithMessage("Max levels cannot exceed 20");

        RuleFor(x => x.MinimumPurchaseAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum purchase amount cannot be negative");

        RuleFor(x => x.CommissionLevels)
            .NotEmpty().WithMessage("At least one commission level is required")
            .Must(HaveValidLevels).WithMessage("Commission levels must be sequential starting from 1")
            .Must(HaveValidPercentages).WithMessage("Commission percentages must be between 0 and 100");

        RuleForEach(x => x.CommissionLevels)
            .SetValidator(new CreateCommissionLevelDtoValidator());
    }

    private bool HaveValidLevels(IEnumerable<CreateCommissionLevelDto> levels)
    {
        if (levels == null || !levels.Any())
            return false;

        var levelNumbers = levels.Select(l => l.Level).OrderBy(l => l).ToList();
        
        // Check if levels are sequential starting from 1
        for (int i = 0; i < levelNumbers.Count; i++)
        {
            if (levelNumbers[i] != i + 1)
                return false;
        }

        return true;
    }

    private bool HaveValidPercentages(IEnumerable<CreateCommissionLevelDto> levels)
    {
        if (levels == null)
            return true;

        return levels.All(l => l.Percentage >= 0 && l.Percentage <= 100);
    }
}

public class UpdateCommissionRuleSetDtoValidator : AbstractValidator<UpdateCommissionRuleSetDto>
{
    public UpdateCommissionRuleSetDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.MaxLevels)
            .GreaterThan(0).WithMessage("Max levels must be greater than 0")
            .LessThanOrEqualTo(20).WithMessage("Max levels cannot exceed 20");

        RuleFor(x => x.MinimumPurchaseAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum purchase amount cannot be negative");

        RuleFor(x => x.CommissionLevels)
            .NotEmpty().WithMessage("At least one commission level is required")
            .Must(HaveValidLevels).WithMessage("Commission levels must be sequential starting from 1")
            .Must(HaveValidPercentages).WithMessage("Commission percentages must be between 0 and 100");

        RuleForEach(x => x.CommissionLevels)
            .SetValidator(new CreateCommissionLevelDtoValidator());
    }

    private bool HaveValidLevels(IEnumerable<CreateCommissionLevelDto> levels)
    {
        if (levels == null || !levels.Any())
            return false;

        var levelNumbers = levels.Select(l => l.Level).OrderBy(l => l).ToList();
        
        // Check if levels are sequential starting from 1
        for (int i = 0; i < levelNumbers.Count; i++)
        {
            if (levelNumbers[i] != i + 1)
                return false;
        }

        return true;
    }

    private bool HaveValidPercentages(IEnumerable<CreateCommissionLevelDto> levels)
    {
        if (levels == null)
            return true;

        return levels.All(l => l.Percentage >= 0 && l.Percentage <= 100);
    }
}

public class CommissionLevelDtoValidator : AbstractValidator<CommissionLevelDto>
{
    public CommissionLevelDtoValidator()
    {
        RuleFor(x => x.Level)
            .GreaterThan(0).WithMessage("Level must be greater than 0");

        RuleFor(x => x.Percentage)
            .GreaterThanOrEqualTo(0).WithMessage("Percentage cannot be negative")
            .LessThanOrEqualTo(100).WithMessage("Percentage cannot exceed 100");

        RuleFor(x => x.FixedAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Fixed amount cannot be negative")
            .When(x => x.FixedAmount.HasValue);

        RuleFor(x => x.MaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Max amount cannot be negative")
            .When(x => x.MaxAmount.HasValue);

        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Min amount cannot be negative")
            .When(x => x.MinAmount.HasValue);

        RuleFor(x => x)
            .Must(HaveValidAmountRange)
            .WithMessage("Min amount cannot be greater than max amount")
            .When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue);

        RuleFor(x => x)
            .Must(HaveEitherPercentageOrFixedAmount)
            .WithMessage("Either percentage or fixed amount must be specified");
    }

    private bool HaveValidAmountRange(CommissionLevelDto level)
    {
        if (!level.MinAmount.HasValue || !level.MaxAmount.HasValue)
            return true;

        return level.MinAmount.Value <= level.MaxAmount.Value;
    }

    private bool HaveEitherPercentageOrFixedAmount(CommissionLevelDto level)
    {
        return level.Percentage > 0 || (level.FixedAmount.HasValue && level.FixedAmount.Value > 0);
    }
}

public class CreateProductRuleBindingDtoValidator : AbstractValidator<CreateProductRuleBindingDto>
{
    public CreateProductRuleBindingDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.ValidFrom)
            .LessThan(x => x.ValidTo)
            .WithMessage("Valid from date must be before valid to date")
            .When(x => x.ValidTo.HasValue);

        RuleFor(x => x.ValidTo)
            .GreaterThan(x => x.ValidFrom)
            .WithMessage("Valid to date must be after valid from date")
            .When(x => x.ValidFrom.HasValue);
    }
}

public class CreateCommissionRuleVersionDtoValidator : AbstractValidator<CreateCommissionRuleVersionDto>
{
    public CreateCommissionRuleVersionDtoValidator()
    {
        RuleFor(x => x.RuleSetId)
            .NotEmpty().WithMessage("Rule set ID is required");

        RuleFor(x => x.SchemaJson)
            .NotEmpty().WithMessage("Schema JSON is required")
            .Must(BeValidJson).WithMessage("Schema must be valid JSON");

        RuleFor(x => x.PublishedBy)
            .NotEmpty().WithMessage("Published by is required")
            .MaximumLength(100).WithMessage("Published by cannot exceed 100 characters");
    }

    private bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class CommissionRuleSetSchemaValidator
{
    public static ValidationResult ValidateSchema(string schemaJson)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            errors.Add("Schema JSON cannot be empty");
            return new ValidationResult(false, errors);
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(schemaJson);
            var root = document.RootElement;

            // Validate required properties
            if (!root.TryGetProperty("name", out _))
                errors.Add("Schema must contain 'name' property");

            if (!root.TryGetProperty("version", out _))
                errors.Add("Schema must contain 'version' property");

            if (!root.TryGetProperty("rules", out var rulesElement) || rulesElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                errors.Add("Schema must contain 'rules' array property");
            else
            {
                // Validate rules array
                foreach (var rule in rulesElement.EnumerateArray())
                {
                    if (!rule.TryGetProperty("level", out var levelElement) || levelElement.ValueKind != System.Text.Json.JsonValueKind.Number)
                        errors.Add("Each rule must have a numeric 'level' property");

                    if (!rule.TryGetProperty("percentage", out var percentageElement) || percentageElement.ValueKind != System.Text.Json.JsonValueKind.Number)
                        errors.Add("Each rule must have a numeric 'percentage' property");
                    else
                    {
                        var percentage = percentageElement.GetDecimal();
                        if (percentage < 0 || percentage > 100)
                            errors.Add("Percentage must be between 0 and 100");
                    }
                }
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            errors.Add($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            errors.Add($"Schema validation error: {ex.Message}");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }

        public ValidationResult(bool isValid, IEnumerable<string> errors)
        {
            IsValid = isValid;
            Errors = errors.ToList().AsReadOnly();
        }
    }
}