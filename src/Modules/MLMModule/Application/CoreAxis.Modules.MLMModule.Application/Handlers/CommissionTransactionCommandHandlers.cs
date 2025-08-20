using CoreAxis.Modules.MLMModule.Application.Commands;
using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using CoreAxis.Modules.MLMModule.Domain.Events;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Handlers;

public class ApproveCommissionCommandHandler : IRequestHandler<ApproveCommissionCommand, CommissionTransactionDto>
{
    private readonly ICommissionTransactionRepository _commissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public ApproveCommissionCommandHandler(
        ICommissionTransactionRepository commissionRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _commissionRepository = commissionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<CommissionTransactionDto> Handle(ApproveCommissionCommand request, CancellationToken cancellationToken)
    {
        var commission = await _commissionRepository.GetByIdAsync(request.CommissionId);
        if (commission == null)
        {
            throw new InvalidOperationException("Commission not found");
        }

        commission.Approve(request.Notes);
        await _commissionRepository.UpdateAsync(commission);
        await _unitOfWork.SaveChangesAsync();

        // Publish domain event
        var approvedEvent = new CommissionApprovedEvent(
            commission.Id,
            commission.UserId,
            commission.Amount);
        await _publisher.Publish(approvedEvent, cancellationToken);

        return MapToDto(commission);
    }

    private static CommissionTransactionDto MapToDto(CommissionTransaction commission)
    {
        return new CommissionTransactionDto
        {
            Id = commission.Id,
            UserId = commission.UserId,
            SourcePaymentId = commission.SourcePaymentId,
            ProductId = commission.ProductId,
            CommissionRuleSetId = commission.CommissionRuleSetId,
            Level = commission.Level,
            Amount = commission.Amount,
            SourceAmount = commission.SourceAmount,
            Percentage = commission.Percentage,
            Status = commission.Status,
            IsSettled = commission.IsSettled,
            WalletTransactionId = commission.WalletTransactionId,
            Notes = commission.Notes,
            CreatedOn = commission.CreatedOn,
            ApprovedAt = commission.ApprovedAt,
            PaidAt = commission.PaidAt,
            RejectedAt = commission.RejectedAt,
            RejectionReason = commission.RejectionReason
        };
    }
}

public class RejectCommissionCommandHandler : IRequestHandler<RejectCommissionCommand, CommissionTransactionDto>
{
    private readonly ICommissionTransactionRepository _commissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public RejectCommissionCommandHandler(
        ICommissionTransactionRepository commissionRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _commissionRepository = commissionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<CommissionTransactionDto> Handle(RejectCommissionCommand request, CancellationToken cancellationToken)
    {
        var commission = await _commissionRepository.GetByIdAsync(request.CommissionId);
        if (commission == null)
        {
            throw new InvalidOperationException("Commission not found");
        }

        commission.Reject(request.RejectedBy.ToString(), request.RejectionReason, request.Notes);
        await _commissionRepository.UpdateAsync(commission);
        await _unitOfWork.SaveChangesAsync();

        // Publish domain event
        var rejectedEvent = new CommissionRejectedEvent(
            commission.Id,
            commission.UserId,
            commission.Amount,
            request.RejectionReason);
        await _publisher.Publish(rejectedEvent, cancellationToken);

        return MapToDto(commission);
    }

    private static CommissionTransactionDto MapToDto(CommissionTransaction commission)
    {
        return new CommissionTransactionDto
        {
            Id = commission.Id,
            UserId = commission.UserId,
            SourcePaymentId = commission.SourcePaymentId,
            ProductId = commission.ProductId,
            CommissionRuleSetId = commission.CommissionRuleSetId,
            Level = commission.Level,
            Amount = commission.Amount,
            SourceAmount = commission.SourceAmount,
            Percentage = commission.Percentage,
            Status = commission.Status,
            IsSettled = commission.IsSettled,
            WalletTransactionId = commission.WalletTransactionId,
            Notes = commission.Notes,
            CreatedOn = commission.CreatedOn,
            ApprovedAt = commission.ApprovedAt,
            PaidAt = commission.PaidAt,
            RejectedAt = commission.RejectedAt,
            RejectionReason = commission.RejectionReason
        };
    }
}

public class MarkCommissionAsPaidCommandHandler : IRequestHandler<MarkCommissionAsPaidCommand, CommissionTransactionDto>
{
    private readonly ICommissionTransactionRepository _commissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public MarkCommissionAsPaidCommandHandler(
        ICommissionTransactionRepository commissionRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _commissionRepository = commissionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<CommissionTransactionDto> Handle(MarkCommissionAsPaidCommand request, CancellationToken cancellationToken)
    {
        var commission = await _commissionRepository.GetByIdAsync(request.CommissionId);
        if (commission == null)
        {
            throw new InvalidOperationException("Commission not found");
        }

        commission.MarkAsPaid(request.WalletTransactionId);
        await _commissionRepository.UpdateAsync(commission);
        await _unitOfWork.SaveChangesAsync();

        // Publish domain event
        var paidEvent = new CommissionPaidEvent(
            commission.Id,
            commission.UserId,
            commission.Amount,
            request.WalletTransactionId);
        await _publisher.Publish(paidEvent, cancellationToken);

        return MapToDto(commission);
    }

    private static CommissionTransactionDto MapToDto(CommissionTransaction commission)
    {
        return new CommissionTransactionDto
        {
            Id = commission.Id,
            UserId = commission.UserId,
            SourcePaymentId = commission.SourcePaymentId,
            ProductId = commission.ProductId,
            CommissionRuleSetId = commission.CommissionRuleSetId,
            Level = commission.Level,
            Amount = commission.Amount,
            SourceAmount = commission.SourceAmount,
            Percentage = commission.Percentage,
            Status = commission.Status,
            IsSettled = commission.IsSettled,
            WalletTransactionId = commission.WalletTransactionId,
            Notes = commission.Notes,
            CreatedOn = commission.CreatedOn,
            ApprovedAt = commission.ApprovedAt,
            PaidAt = commission.PaidAt,
            RejectedAt = commission.RejectedAt,
            RejectionReason = commission.RejectionReason
        };
    }
}

public class ProcessPendingCommissionsCommandHandler : IRequestHandler<ProcessPendingCommissionsCommand, List<CommissionTransactionDto>>
{
    private readonly ICommissionTransactionRepository _commissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessPendingCommissionsCommandHandler(
        ICommissionTransactionRepository commissionRepository,
        IUnitOfWork unitOfWork)
    {
        _commissionRepository = commissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CommissionTransactionDto>> Handle(ProcessPendingCommissionsCommand request, CancellationToken cancellationToken)
    {
        var pendingCommissions = await _commissionRepository.GetPendingForApprovalAsync(request.BatchSize);
        var processedCommissions = new List<CommissionTransactionDto>();

        foreach (var commission in pendingCommissions)
        {
            // Auto-approve based on business rules (can be customized)
            commission.Approve("Auto-approved by system");
            await _commissionRepository.UpdateAsync(commission);
            
            processedCommissions.Add(MapToDto(commission));
        }

        await _unitOfWork.SaveChangesAsync();
        return processedCommissions;
    }

    private static CommissionTransactionDto MapToDto(CommissionTransaction commission)
    {
        return new CommissionTransactionDto
        {
            Id = commission.Id,
            UserId = commission.UserId,
            SourcePaymentId = commission.SourcePaymentId,
            ProductId = commission.ProductId,
            CommissionRuleSetId = commission.CommissionRuleSetId,
            Level = commission.Level,
            Amount = commission.Amount,
            SourceAmount = commission.SourceAmount,
            Percentage = commission.Percentage,
            Status = commission.Status,
            IsSettled = commission.IsSettled,
            WalletTransactionId = commission.WalletTransactionId,
            Notes = commission.Notes,
            CreatedOn = commission.CreatedOn,
            ApprovedAt = commission.ApprovedAt,
            PaidAt = commission.PaidAt,
            RejectedAt = commission.RejectedAt,
            RejectionReason = commission.RejectionReason
        };
    }
}

public class UpdateCommissionNotesCommandHandler : IRequestHandler<UpdateCommissionNotesCommand, CommissionTransactionDto>
{
    private readonly ICommissionTransactionRepository _commissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCommissionNotesCommandHandler(
        ICommissionTransactionRepository commissionRepository,
        IUnitOfWork unitOfWork)
    {
        _commissionRepository = commissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CommissionTransactionDto> Handle(UpdateCommissionNotesCommand request, CancellationToken cancellationToken)
    {
        var commission = await _commissionRepository.GetByIdAsync(request.CommissionId);
        if (commission == null)
        {
            throw new InvalidOperationException("Commission not found");
        }

        commission.UpdateNotes(request.Notes);
        await _commissionRepository.UpdateAsync(commission);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(commission);
    }

    private static CommissionTransactionDto MapToDto(CommissionTransaction commission)
    {
        return new CommissionTransactionDto
        {
            Id = commission.Id,
            UserId = commission.UserId,
            SourcePaymentId = commission.SourcePaymentId,
            ProductId = commission.ProductId,
            CommissionRuleSetId = commission.CommissionRuleSetId,
            Level = commission.Level,
            Amount = commission.Amount,
            SourceAmount = commission.SourceAmount,
            Percentage = commission.Percentage,
            Status = commission.Status,
            IsSettled = commission.IsSettled,
            WalletTransactionId = commission.WalletTransactionId,
            Notes = commission.Notes,
            CreatedOn = commission.CreatedOn,
            ApprovedAt = commission.ApprovedAt,
            PaidAt = commission.PaidAt,
            RejectedAt = commission.RejectedAt,
            RejectionReason = commission.RejectionReason
        };
    }
}

public class ExpireCommissionCommandHandler : IRequestHandler<ExpireCommissionCommand, CommissionTransactionDto>
{
    private readonly ICommissionTransactionRepository _commissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public ExpireCommissionCommandHandler(
        ICommissionTransactionRepository commissionRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _commissionRepository = commissionRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<CommissionTransactionDto> Handle(ExpireCommissionCommand request, CancellationToken cancellationToken)
    {
        var commission = await _commissionRepository.GetByIdAsync(request.CommissionId);
        if (commission == null)
        {
            throw new InvalidOperationException("Commission not found");
        }

        commission.MarkAsExpired();
        await _commissionRepository.UpdateAsync(commission);
        await _unitOfWork.SaveChangesAsync();

        // Publish domain event
        var expiredEvent = new CommissionExpiredEvent(
            commission.Id,
            commission.UserId,
            commission.Amount);
        await _publisher.Publish(expiredEvent, cancellationToken);

        return MapToDto(commission);
    }

    private static CommissionTransactionDto MapToDto(CommissionTransaction commission)
    {
        return new CommissionTransactionDto
        {
            Id = commission.Id,
            UserId = commission.UserId,
            SourcePaymentId = commission.SourcePaymentId,
            ProductId = commission.ProductId,
            CommissionRuleSetId = commission.CommissionRuleSetId,
            Level = commission.Level,
            Amount = commission.Amount,
            SourceAmount = commission.SourceAmount,
            Percentage = commission.Percentage,
            Status = commission.Status,
            IsSettled = commission.IsSettled,
            WalletTransactionId = commission.WalletTransactionId,
            Notes = commission.Notes,
            CreatedOn = commission.CreatedOn,
            ApprovedAt = commission.ApprovedAt,
            PaidAt = commission.PaidAt,
            RejectedAt = commission.RejectedAt,
            RejectionReason = commission.RejectionReason
        };
    }
}