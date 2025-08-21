using CoreAxis.Modules.CommerceModule.Domain.Entities;
using FluentValidation;

namespace CoreAxis.Modules.CommerceModule.Domain.Validators;

/// <summary>
/// Validator for SplitPaymentRule entity.
/// </summary>
public class SplitPaymentRuleValidator : AbstractValidator<SplitPaymentRule>
{
    public SplitPaymentRuleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Split payment rule name is required.")
            .MaximumLength(200)
            .WithMessage("Split payment rule name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Priority must be a non-negative number.");

        RuleFor(x => x.SplitConfigurationJson)
            .NotEmpty()
            .WithMessage("Split configuration is required.")
            .Must(BeValidJson)
            .WithMessage("Split configuration must be valid JSON.");

        RuleFor(x => x.ConditionsJson)
            .Must(BeValidJson)
            .WithMessage("Conditions must be valid JSON.")
            .When(x => !string.IsNullOrEmpty(x.ConditionsJson));

        RuleFor(x => x.MetadataJson)
            .Must(BeValidJson)
            .WithMessage("Metadata must be valid JSON.")
            .When(x => !string.IsNullOrEmpty(x.MetadataJson));

        RuleFor(x => x)
            .Must(HaveValidDateRange)
            .WithMessage("Effective date must be before expiry date.")
            .When(x => x.EffectiveDate.HasValue && x.ExpiryDate.HasValue);

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Expiry date must be in the future.")
            .When(x => x.ExpiryDate.HasValue);
    }

    /// <summary>
    /// Validates if the provided string is valid JSON.
    /// </summary>
    private static bool BeValidJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return true;

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

    /// <summary>
    /// Validates that effective date is before expiry date.
    /// </summary>
    private static bool HaveValidDateRange(SplitPaymentRule rule)
    {
        if (!rule.EffectiveDate.HasValue || !rule.ExpiryDate.HasValue)
            return true;

        return rule.EffectiveDate.Value < rule.ExpiryDate.Value;
    }
}