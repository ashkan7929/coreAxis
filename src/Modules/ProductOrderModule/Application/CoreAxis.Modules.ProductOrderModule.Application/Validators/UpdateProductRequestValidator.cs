using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using FluentValidation;

namespace CoreAxis.Modules.ProductOrderModule.Application.Validators;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 200).WithMessage("Name must be between 2 and 200 characters");

        RuleFor(x => x.PriceFrom)
            .GreaterThanOrEqualTo(0).When(x => x.PriceFrom.HasValue)
            .WithMessage("PriceFrom cannot be negative");

        RuleFor(x => x.Currency)
            .Length(3).WithMessage("Currency must be a 3-letter code")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));

        RuleFor(x => x.Attributes)
            .Must(attrs => attrs == null || attrs.All(kv => !string.IsNullOrWhiteSpace(kv.Key)))
            .WithMessage("Attribute keys must be non-empty")
            .Must(attrs => attrs == null || attrs.All(kv => kv.Key.Length <= 100))
            .WithMessage("Attribute keys must be at most 100 characters")
            .Must(attrs => attrs == null || attrs.All(kv => kv.Value.Length <= 500))
            .WithMessage("Attribute values must be at most 500 characters");
    }
}