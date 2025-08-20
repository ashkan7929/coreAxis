using CoreAxis.Modules.MLMModule.Application.Contracts;
using FluentValidation;

namespace CoreAxis.Modules.MLMModule.Application.Validators;

public class GetDownlineRequestValidator : AbstractValidator<GetDownlineRequest>
{
    public GetDownlineRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100.");

        RuleFor(x => x.MaxDepth)
            .GreaterThan(0)
            .WithMessage("Max depth must be greater than 0.")
            .LessThanOrEqualTo(10)
            .WithMessage("Max depth cannot exceed 10.");
    }
}