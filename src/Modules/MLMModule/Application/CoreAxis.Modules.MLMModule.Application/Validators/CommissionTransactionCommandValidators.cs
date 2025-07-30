using CoreAxis.Modules.MLMModule.Application.Commands;
using FluentValidation;

namespace CoreAxis.Modules.MLMModule.Application.Validators;

public class ApproveCommissionCommandValidator : AbstractValidator<ApproveCommissionCommand>
{
    public ApproveCommissionCommandValidator()
    {
        RuleFor(x => x.CommissionId)
            .NotEmpty()
            .WithMessage("Commission ID is required.");



        RuleFor(x => x.ApprovedBy)
            .NotEmpty()
            .WithMessage("Approved by user ID is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class RejectCommissionCommandValidator : AbstractValidator<RejectCommissionCommand>
{
    public RejectCommissionCommandValidator()
    {
        RuleFor(x => x.CommissionId)
            .NotEmpty()
            .WithMessage("Commission ID is required.");



        RuleFor(x => x.RejectedBy)
            .NotEmpty()
            .WithMessage("Rejected by user ID is required.");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .WithMessage("Rejection reason is required.")
            .MaximumLength(500)
            .WithMessage("Rejection reason cannot exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class MarkCommissionAsPaidCommandValidator : AbstractValidator<MarkCommissionAsPaidCommand>
{
    public MarkCommissionAsPaidCommandValidator()
    {
        RuleFor(x => x.CommissionId)
            .NotEmpty()
            .WithMessage("Commission ID is required.");



        RuleFor(x => x.PaidBy)
            .NotEmpty()
            .WithMessage("Paid by user ID is required.");

        RuleFor(x => x.WalletTransactionId)
            .NotEmpty()
            .WithMessage("Wallet transaction ID is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class ProcessPendingCommissionsCommandValidator : AbstractValidator<ProcessPendingCommissionsCommand>
{
    public ProcessPendingCommissionsCommandValidator()
    {


        RuleFor(x => x.ProcessedBy)
            .NotEmpty()
            .WithMessage("Processed by user ID is required.");

        RuleFor(x => x.BatchSize)
            .GreaterThan(0)
            .WithMessage("Batch size must be greater than 0.")
            .LessThanOrEqualTo(1000)
            .WithMessage("Batch size cannot exceed 1000.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class UpdateCommissionNotesCommandValidator : AbstractValidator<UpdateCommissionNotesCommand>
{
    public UpdateCommissionNotesCommandValidator()
    {
        RuleFor(x => x.CommissionId)
            .NotEmpty()
            .WithMessage("Commission ID is required.");



        RuleFor(x => x.Notes)
            .NotEmpty()
            .WithMessage("Notes are required.")
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class ExpireCommissionCommandValidator : AbstractValidator<ExpireCommissionCommand>
{
    public ExpireCommissionCommandValidator()
    {
        RuleFor(x => x.CommissionId)
            .NotEmpty()
            .WithMessage("Commission ID is required.");


    }
}