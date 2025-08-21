using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using FluentValidation;

namespace CoreAxis.Modules.CommerceModule.Domain.Validators;

/// <summary>
/// Validator for RefundRequest entity.
/// </summary>
public class RefundRequestValidator : AbstractValidator<RefundRequest>
{
    public RefundRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required.");

        RuleFor(x => x.RefundNumber)
            .NotEmpty()
            .WithMessage("Refund number is required.")
            .MaximumLength(50)
            .WithMessage("Refund number cannot exceed 50 characters.");

        RuleFor(x => x.RefundType)
            .IsInEnum()
            .WithMessage("Invalid refund type.");

        RuleFor(x => x.Reason)
            .IsInEnum()
            .WithMessage("Invalid refund reason.");

        RuleFor(x => x.ReasonDescription)
            .MaximumLength(1000)
            .WithMessage("Reason description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.ReasonDescription));

        RuleFor(x => x.OriginalAmount)
            .GreaterThan(0)
            .WithMessage("Original amount must be greater than zero.");

        RuleFor(x => x.RequestedAmount)
            .GreaterThan(0)
            .WithMessage("Requested amount must be greater than zero.")
            .LessThanOrEqualTo(x => x.OriginalAmount)
            .WithMessage("Requested amount cannot exceed original amount.");

        RuleFor(x => x.ApprovedAmount)
            .GreaterThan(0)
            .WithMessage("Approved amount must be greater than zero.")
            .LessThanOrEqualTo(x => x.OriginalAmount)
            .WithMessage("Approved amount cannot exceed original amount.")
            .When(x => x.ApprovedAmount.HasValue);

        RuleFor(x => x.RefundedAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Refunded amount cannot be negative.")
            .LessThanOrEqualTo(x => x.OriginalAmount)
            .WithMessage("Refunded amount cannot exceed original amount.");

        RuleFor(x => x.FeeAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Fee amount cannot be negative.");

        RuleFor(x => x.NetRefundAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Net refund amount cannot be negative.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency must be a 3-character ISO code.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid refund status.");

        RuleFor(x => x.RequestedAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Requested date cannot be in the future.");

        RuleFor(x => x.RefundMethod)
            .IsInEnum()
            .WithMessage("Invalid refund method.");

        RuleFor(x => x.RefundDestination)
            .MaximumLength(100)
            .WithMessage("Refund destination cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.RefundDestination));

        RuleFor(x => x.ExternalRefundId)
            .MaximumLength(100)
            .WithMessage("External refund ID cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.ExternalRefundId));

        RuleFor(x => x.PaymentProvider)
            .MaximumLength(50)
            .WithMessage("Payment provider cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.PaymentProvider));

        RuleFor(x => x.ErrorMessage)
            .MaximumLength(1000)
            .WithMessage("Error message cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.ErrorMessage));

        RuleFor(x => x.RetryAttempts)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Retry attempts cannot be negative.")
            .LessThanOrEqualTo(x => x.MaxRetryAttempts)
            .WithMessage("Retry attempts cannot exceed maximum retry attempts.");

        RuleFor(x => x.MaxRetryAttempts)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Maximum retry attempts cannot be negative.");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid refund priority.");

        RuleFor(x => x.CorrelationId)
            .MaximumLength(100)
            .WithMessage("Correlation ID cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.CorrelationId));

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(100)
            .WithMessage("Idempotency key cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.IdempotencyKey));

        RuleFor(x => x.InternalNotes)
            .MaximumLength(2000)
            .WithMessage("Internal notes cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.InternalNotes));

        RuleFor(x => x.MetadataJson)
            .Must(BeValidJson)
            .WithMessage("Metadata must be valid JSON.")
            .When(x => !string.IsNullOrEmpty(x.MetadataJson));

        RuleFor(x => x)
            .Must(HaveValidDateSequence)
            .WithMessage("Date sequence is invalid (requested -> approved -> processed -> completed).");

        RuleFor(x => x)
            .Must(HaveValidNetAmount)
            .WithMessage("Net refund amount should equal refunded amount minus fee amount.");

        RuleFor(x => x.ApprovedAt)
            .GreaterThanOrEqualTo(x => x.RequestedAt)
            .WithMessage("Approved date cannot be before requested date.")
            .When(x => x.ApprovedAt.HasValue);

        RuleFor(x => x.ProcessedAt)
            .GreaterThanOrEqualTo(x => x.ApprovedAt)
            .WithMessage("Processed date cannot be before approved date.")
            .When(x => x.ProcessedAt.HasValue && x.ApprovedAt.HasValue);

        RuleFor(x => x.CompletedAt)
            .GreaterThanOrEqualTo(x => x.ProcessedAt)
            .WithMessage("Completed date cannot be before processed date.")
            .When(x => x.CompletedAt.HasValue && x.ProcessedAt.HasValue);
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
    /// Validates that the date sequence is logical.
    /// </summary>
    private static bool HaveValidDateSequence(RefundRequest refund)
    {
        var dates = new List<DateTime?> { refund.RequestedAt, refund.ApprovedAt, refund.ProcessedAt, refund.CompletedAt };
        
        DateTime? previousDate = null;
        foreach (var date in dates)
        {
            if (date.HasValue)
            {
                if (previousDate.HasValue && date.Value < previousDate.Value)
                    return false;
                previousDate = date.Value;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Validates that net refund amount equals refunded amount minus fee amount.
    /// </summary>
    private static bool HaveValidNetAmount(RefundRequest refund)
    {
        var expectedNetAmount = refund.RefundedAmount - refund.FeeAmount;
        return Math.Abs(refund.NetRefundAmount - expectedNetAmount) < 0.01m; // Allow for small rounding differences
    }
}