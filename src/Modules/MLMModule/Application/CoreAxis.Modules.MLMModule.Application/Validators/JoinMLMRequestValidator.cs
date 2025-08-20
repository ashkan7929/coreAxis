using CoreAxis.Modules.MLMModule.Application.Contracts;
using FluentValidation;

namespace CoreAxis.Modules.MLMModule.Application.Validators;

public class JoinMLMRequestValidator : AbstractValidator<JoinMLMRequest>
{
    public JoinMLMRequestValidator()
    {
        RuleFor(x => x.ReferralCode)
            .NotEmpty()
            .WithMessage("Referral code is required.")
            .Length(3, 20)
            .WithMessage("Referral code must be between 3 and 20 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReferralCode));
    }
}