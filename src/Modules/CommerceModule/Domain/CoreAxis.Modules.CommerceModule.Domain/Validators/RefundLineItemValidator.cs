using CoreAxis.Modules.CommerceModule.Domain.Entities;
using FluentValidation;

namespace CoreAxis.Modules.CommerceModule.Domain.Validators;

/// <summary>
/// Validator for RefundLineItem entity.
/// </summary>
public class RefundLineItemValidator : AbstractValidator<RefundLineItem>
{
    public RefundLineItemValidator()
    {
        RuleFor(x => x.RefundRequestId)
            .NotEmpty()
            .WithMessage("Refund request ID is required.");

        RuleFor(x => x.OrderLineItemId)
            .NotEmpty()
            .WithMessage("Order line item ID is required.");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.ProductSku)
            .NotEmpty()
            .WithMessage("Product SKU is required.")
            .MaximumLength(100)
            .WithMessage("Product SKU cannot exceed 100 characters.");

        RuleFor(x => x.OriginalQuantity)
            .GreaterThan(0)
            .WithMessage("Original quantity must be greater than zero.");

        RuleFor(x => x.RefundQuantity)
            .GreaterThan(0)
            .WithMessage("Refund quantity must be greater than zero.")
            .LessThanOrEqualTo(x => x.OriginalQuantity)
            .WithMessage("Refund quantity cannot exceed original quantity.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Unit price cannot be negative.");

        RuleFor(x => x.RefundAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Refund amount cannot be negative.");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Tax amount cannot be negative.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Discount amount cannot be negative.");

        RuleFor(x => x.NetRefundAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Net refund amount cannot be negative.");

        RuleFor(x => x.ItemRefundReason)
            .MaximumLength(500)
            .WithMessage("Item refund reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ItemRefundReason));

        RuleFor(x => x.ReturnCondition)
            .MaximumLength(200)
            .WithMessage("Return condition cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.ReturnCondition));

        RuleFor(x => x.MetadataJson)
            .Must(BeValidJson)
            .WithMessage("Metadata must be valid JSON.")
            .When(x => !string.IsNullOrEmpty(x.MetadataJson));

        RuleFor(x => x)
            .Must(HaveValidRefundAmount)
            .WithMessage("Refund amount should be calculated correctly based on quantity and unit price.");

        RuleFor(x => x)
            .Must(HaveValidNetRefundAmount)
            .WithMessage("Net refund amount should equal refund amount minus tax amount plus discount amount.");

        RuleFor(x => x.ReturnedAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Returned date cannot be in the future.")
            .When(x => x.ReturnedAt.HasValue);

        RuleFor(x => x)
            .Must(HaveValidReturnStatus)
            .WithMessage("If item is marked as returned, returned date must be set.");
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
    /// Validates that refund amount is calculated correctly.
    /// </summary>
    private static bool HaveValidRefundAmount(RefundLineItem item)
    {
        var expectedRefundAmount = item.RefundQuantity * item.UnitPrice;
        return Math.Abs(item.RefundAmount - expectedRefundAmount) < 0.01m; // Allow for small rounding differences
    }

    /// <summary>
    /// Validates that net refund amount is calculated correctly.
    /// </summary>
    private static bool HaveValidNetRefundAmount(RefundLineItem item)
    {
        var expectedNetAmount = item.RefundAmount - item.TaxAmount + item.DiscountAmount;
        return Math.Abs(item.NetRefundAmount - expectedNetAmount) < 0.01m; // Allow for small rounding differences
    }

    /// <summary>
    /// Validates that return status is consistent.
    /// </summary>
    private static bool HaveValidReturnStatus(RefundLineItem item)
    {
        if (item.IsReturned && !item.ReturnedAt.HasValue)
            return false;
            
        if (!item.IsReturned && item.ReturnedAt.HasValue)
            return false;
            
        return true;
    }
}