using CoreAxis.Modules.CommerceModule.Domain.Entities;
using FluentValidation;

namespace CoreAxis.Modules.CommerceModule.Domain.Validators;

/// <summary>
/// Validator for CouponRedemption entity.
/// </summary>
public class CouponRedemptionValidator : AbstractValidator<CouponRedemption>
{
    public CouponRedemptionValidator()
    {
        RuleFor(x => x.DiscountRuleId)
            .NotEmpty().WithMessage("Discount rule ID is required.");

        RuleFor(x => x.CouponCode)
            .NotEmpty().WithMessage("Coupon code is required.")
            .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters.")
            .Matches("^[A-Z0-9-_]+$").WithMessage("Coupon code can only contain uppercase letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.OriginalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Original amount cannot be negative.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount amount cannot be negative.");

        RuleFor(x => x.FinalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Final amount cannot be negative.");

        RuleFor(x => x.RedeemedAt)
            .NotEmpty().WithMessage("Redeemed date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5)).WithMessage("Redeemed date cannot be in the future.");

        RuleFor(x => x.IpAddress)
            .MaximumLength(45).WithMessage("IP address cannot exceed 45 characters.")
            .Must(BeValidIpAddress)
            .When(x => !string.IsNullOrEmpty(x.IpAddress))
            .WithMessage("Invalid IP address format.");

        RuleFor(x => x.UserAgent)
            .MaximumLength(500).WithMessage("User agent cannot exceed 500 characters.");

        RuleFor(x => x.CorrelationId)
            .MaximumLength(100).WithMessage("Correlation ID cannot exceed 100 characters.");

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(100).WithMessage("Idempotency key cannot exceed 100 characters.");

        // Custom validation for amount consistency
        RuleFor(x => x)
            .Must(HaveConsistentAmounts)
            .WithMessage("Final amount must equal original amount minus discount amount.");

        RuleFor(x => x)
            .Must(HaveReasonableDiscountAmount)
            .WithMessage("Discount amount cannot exceed original amount.");
    }

    private static bool BeValidIpAddress(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return true;

        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }

    private static bool HaveConsistentAmounts(CouponRedemption redemption)
    {
        var expectedFinalAmount = redemption.OriginalAmount - redemption.DiscountAmount;
        return Math.Abs(redemption.FinalAmount - expectedFinalAmount) < 0.01m; // Allow for small rounding differences
    }

    private static bool HaveReasonableDiscountAmount(CouponRedemption redemption)
    {
        return redemption.DiscountAmount <= redemption.OriginalAmount;
    }
}