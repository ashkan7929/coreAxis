using CoreAxis.Modules.MLMModule.Application.Commands;
using FluentValidation;

namespace CoreAxis.Modules.MLMModule.Application.Validators;

public class CreateUserReferralCommandValidator : AbstractValidator<CreateUserReferralCommand>
{
    public CreateUserReferralCommandValidator()
    {


        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.ParentUserId)
            .NotEqual(x => x.UserId)
            .When(x => x.ParentUserId.HasValue)
            .WithMessage("User cannot refer themselves.");
    }
}

public class UpdateUserReferralCommandValidator : AbstractValidator<UpdateUserReferralCommand>
{
    public UpdateUserReferralCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Referral ID is required.");


    }
}

public class ActivateUserReferralCommandValidator : AbstractValidator<ActivateUserReferralCommand>
{
    public ActivateUserReferralCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");


    }
}

public class DeactivateUserReferralCommandValidator : AbstractValidator<DeactivateUserReferralCommand>
{
    public DeactivateUserReferralCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");


    }
}

public class DeleteUserReferralCommandValidator : AbstractValidator<DeleteUserReferralCommand>
{
    public DeleteUserReferralCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Referral ID is required.");


    }
}