using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using FluentValidation;

namespace CoreAxis.Modules.CommerceModule.Domain.Validators;

/// <summary>
/// Validator for SplitPaymentAllocation entity.
/// </summary>
public class SplitPaymentAllocationValidator : AbstractValidator<SplitPaymentAllocation>
{
    public SplitPaymentAllocationValidator()
    {
        RuleFor(x => x.SplitPaymentRuleId)
            .NotEmpty()
            .WithMessage("Split payment rule ID is required.");

        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required.");

        RuleFor(x => x.PartyId)
            .NotEmpty()
            .WithMessage("Party ID is required.");

        RuleFor(x => x.PartyType)
            .IsInEnum()
            .WithMessage("Invalid party type.");

        RuleFor(x => x.AllocationType)
            .IsInEnum()
            .WithMessage("Invalid allocation type.");

        RuleFor(x => x.AllocationValue)
            .GreaterThan(0)
            .WithMessage("Allocation value must be greater than zero.")
            .Must((allocation, value) => BeValidAllocationValue(allocation.AllocationType, value))
            .WithMessage("Allocation value is invalid for the specified allocation type.");

        RuleFor(x => x.AllocatedAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Allocated amount cannot be negative.");

        RuleFor(x => x.BaseAmount)
            .GreaterThan(0)
            .WithMessage("Base amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency must be a 3-character ISO code.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid allocation status.");

        RuleFor(x => x.FeeAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Fee amount cannot be negative.");

        RuleFor(x => x.NetAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Net amount cannot be negative.");

        RuleFor(x => x.RetryAttempts)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Retry attempts cannot be negative.");

        RuleFor(x => x.ExternalTransactionId)
            .MaximumLength(100)
            .WithMessage("External transaction ID cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.ExternalTransactionId));

        RuleFor(x => x.ErrorMessage)
            .MaximumLength(1000)
            .WithMessage("Error message cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.ErrorMessage));

        RuleFor(x => x.CorrelationId)
            .MaximumLength(100)
            .WithMessage("Correlation ID cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.CorrelationId));

        RuleFor(x => x.MetadataJson)
            .Must(BeValidJson)
            .WithMessage("Metadata must be valid JSON.")
            .When(x => !string.IsNullOrEmpty(x.MetadataJson));

        RuleFor(x => x)
            .Must(HaveValidAmountRelationship)
            .WithMessage("Allocated amount cannot exceed base amount for percentage allocations.");

        RuleFor(x => x)
            .Must(HaveValidNetAmount)
            .WithMessage("Net amount should equal allocated amount minus fee amount.");

        RuleFor(x => x.ProcessedAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Processed date cannot be in the future.")
            .When(x => x.ProcessedAt.HasValue);

        RuleFor(x => x.SettledAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Settled date cannot be in the future.")
            .GreaterThanOrEqualTo(x => x.ProcessedAt)
            .WithMessage("Settled date cannot be before processed date.")
            .When(x => x.SettledAt.HasValue && x.ProcessedAt.HasValue);
    }

    /// <summary>
    /// Validates if the allocation value is appropriate for the allocation type.
    /// </summary>
    private static bool BeValidAllocationValue(SplitPaymentAllocationType allocationType, decimal value)
    {
        return allocationType switch
        {
            SplitPaymentAllocationType.Percentage => value > 0 && value <= 100,
            SplitPaymentAllocationType.FixedAmount => value > 0,
            SplitPaymentAllocationType.PerUnit => value > 0,
            SplitPaymentAllocationType.Remainder => true, // Remainder doesn't use allocation value
            _ => true
        };
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
    /// Validates that allocated amount doesn't exceed base amount for percentage allocations.
    /// </summary>
    private static bool HaveValidAmountRelationship(SplitPaymentAllocation allocation)
    {
        if (allocation.AllocationType == SplitPaymentAllocationType.Percentage)
        {
            return allocation.AllocatedAmount <= allocation.BaseAmount;
        }
        return true;
    }

    /// <summary>
    /// Validates that net amount equals allocated amount minus fee amount.
    /// </summary>
    private static bool HaveValidNetAmount(SplitPaymentAllocation allocation)
    {
        var expectedNetAmount = allocation.AllocatedAmount - allocation.FeeAmount;
        return Math.Abs(allocation.NetAmount - expectedNetAmount) < 0.01m; // Allow for small rounding differences
    }
}