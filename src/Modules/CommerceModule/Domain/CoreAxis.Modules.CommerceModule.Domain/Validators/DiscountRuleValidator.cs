using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using FluentValidation;

namespace CoreAxis.Modules.CommerceModule.Domain.Validators;

/// <summary>
/// Validator for DiscountRule entity.
/// </summary>
public class DiscountRuleValidator : AbstractValidator<DiscountRule>
{
    public DiscountRuleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Discount rule name is required.")
            .MaximumLength(200).WithMessage("Discount rule name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.Scope)
            .IsInEnum().WithMessage("Invalid discount scope.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid discount type.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0.");

        // For percentage discounts, value should not exceed 100
        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.Type == DiscountType.Percent)
            .WithMessage("Percentage discount cannot exceed 100%.");

        RuleFor(x => x.MaxDiscountAmount)
            .GreaterThan(0)
            .When(x => x.MaxDiscountAmount.HasValue)
            .WithMessage("Maximum discount amount must be greater than 0.");

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinOrderAmount.HasValue)
            .WithMessage("Minimum order amount cannot be negative.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be after start date.");

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0)
            .When(x => x.UsageLimit.HasValue)
            .WithMessage("Usage limit must be greater than 0.");

        RuleFor(x => x.UsageLimitPerCustomer)
            .GreaterThan(0)
            .When(x => x.UsageLimitPerCustomer.HasValue)
            .WithMessage("Usage limit per customer must be greater than 0.");

        RuleFor(x => x.UsageLimitPerCustomer)
            .LessThanOrEqualTo(x => x.UsageLimit)
            .When(x => x.UsageLimit.HasValue && x.UsageLimitPerCustomer.HasValue)
            .WithMessage("Usage limit per customer cannot exceed total usage limit.");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Priority cannot be negative.");

        RuleFor(x => x.CouponCode)
            .NotEmpty()
            .When(x => x.RequiresCoupon)
            .WithMessage("Coupon code is required when RequiresCoupon is true.");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.CouponCode))
            .WithMessage("Coupon code cannot exceed 50 characters.");

        RuleFor(x => x.CouponCode)
            .Matches("^[A-Z0-9-_]+$")
            .When(x => !string.IsNullOrEmpty(x.CouponCode))
            .WithMessage("Coupon code can only contain uppercase letters, numbers, hyphens, and underscores.");

        // Custom validation for date ranges
        RuleFor(x => x)
            .Must(BeValidDateRange)
            .WithMessage("Discount rule must have a valid date range.");

        // Custom validation for usage counts
        RuleFor(x => x)
            .Must(HaveValidUsageCounts)
            .WithMessage("Usage count cannot exceed usage limit.");
    }

    private static bool BeValidDateRange(DiscountRule rule)
    {
        if (!rule.StartDate.HasValue && !rule.EndDate.HasValue)
            return true; // No date restrictions

        if (rule.StartDate.HasValue && rule.EndDate.HasValue)
            return rule.EndDate.Value > rule.StartDate.Value;

        if (rule.EndDate.HasValue)
            return rule.EndDate.Value > DateTime.UtcNow;

        return true; // Only start date is set, which is valid
    }

    private static bool HaveValidUsageCounts(DiscountRule rule)
    {
        if (rule.UsageLimit.HasValue)
            return rule.UsageCount <= rule.UsageLimit.Value;

        return true;
    }
}