using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Queries;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Handlers;

public class GetCommissionByIdQueryHandler : IRequestHandler<GetCommissionByIdQuery, CommissionTransactionDto?>
{
    private readonly ICommissionTransactionRepository _commissionRepository;

    public GetCommissionByIdQueryHandler(ICommissionTransactionRepository commissionRepository)
    {
        _commissionRepository = commissionRepository;
    }

    public async Task<CommissionTransactionDto?> Handle(GetCommissionByIdQuery request, CancellationToken cancellationToken)
    {
        var commission = await _commissionRepository.GetByIdAsync(request.CommissionId);
        if (commission == null)
        {
            return null;
        }

        return MapToDto(commission);
    }

    private static CommissionTransactionDto MapToDto(Domain.Entities.CommissionTransaction commission)
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

public class GetUserCommissionsQueryHandler : IRequestHandler<GetUserCommissionsQuery, IEnumerable<CommissionTransactionDto>>
{
    private readonly ICommissionTransactionRepository _commissionRepository;

    public GetUserCommissionsQueryHandler(ICommissionTransactionRepository commissionRepository)
    {
        _commissionRepository = commissionRepository;
    }

    public async Task<IEnumerable<CommissionTransactionDto>> Handle(GetUserCommissionsQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.Filter.PageNumber - 1) * request.Filter.PageSize;
        var commissions = await _commissionRepository.GetByUserIdAsync(
            request.UserId, 
            skip,
            request.Filter.PageSize,
            cancellationToken);

        return commissions.Select(MapToDto);
    }

    private static CommissionTransactionDto MapToDto(Domain.Entities.CommissionTransaction commission)
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

public class GetCommissionsByStatusQueryHandler : IRequestHandler<GetCommissionsByStatusQuery, IEnumerable<CommissionTransactionDto>>
{
    private readonly ICommissionTransactionRepository _commissionRepository;

    public GetCommissionsByStatusQueryHandler(ICommissionTransactionRepository commissionRepository)
    {
        _commissionRepository = commissionRepository;
    }

    public async Task<IEnumerable<CommissionTransactionDto>> Handle(GetCommissionsByStatusQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.PageNumber - 1) * request.PageSize;
        var commissions = await _commissionRepository.GetByStatusAsync(
            request.Status,
            skip,
            request.PageSize,
            cancellationToken);

        return commissions.Select(MapToDto);
    }

    private static CommissionTransactionDto MapToDto(Domain.Entities.CommissionTransaction commission)
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

public class GetCommissionsBySourcePaymentQueryHandler : IRequestHandler<GetCommissionsBySourcePaymentQuery, IEnumerable<CommissionTransactionDto>>
{
    private readonly ICommissionTransactionRepository _commissionRepository;

    public GetCommissionsBySourcePaymentQueryHandler(ICommissionTransactionRepository commissionRepository)
    {
        _commissionRepository = commissionRepository;
    }

    public async Task<IEnumerable<CommissionTransactionDto>> Handle(GetCommissionsBySourcePaymentQuery request, CancellationToken cancellationToken)
    {
        var commissions = await _commissionRepository.GetBySourcePaymentIdAsync(request.SourcePaymentId, cancellationToken);
        return commissions.Select(MapToDto);
    }

    private static CommissionTransactionDto MapToDto(Domain.Entities.CommissionTransaction commission)
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

public class GetCommissionSummaryQueryHandler : IRequestHandler<GetCommissionSummaryQuery, CommissionSummaryDto>
{
    private readonly ICommissionTransactionRepository _commissionRepository;

    public GetCommissionSummaryQueryHandler(ICommissionTransactionRepository commissionRepository)
    {
        _commissionRepository = commissionRepository;
    }

    public async Task<CommissionSummaryDto> Handle(GetCommissionSummaryQuery request, CancellationToken cancellationToken)
    {
        var totalEarnings = await _commissionRepository.GetTotalEarningsAsync(
            request.UserId, 
            request.FromDate, 
            request.ToDate);

        var totalPending = await _commissionRepository.GetTotalPendingAsync(
            request.UserId, 
            cancellationToken);

        // Get transaction counts by status
        var allCommissions = await _commissionRepository.GetByUserIdAsync(
            request.UserId,
            0, // skip
            int.MaxValue, // take all
            cancellationToken);

        var commissionsList = allCommissions.ToList();
        
        return new CommissionSummaryDto
        {
            TotalEarnings = totalEarnings,
            PendingAmount = totalPending,
            PaidAmount = commissionsList.Where(c => c.Status == CommissionStatus.Paid).Sum(c => c.Amount),
            TotalTransactions = commissionsList.Count,
            PendingTransactions = commissionsList.Count(c => c.Status == CommissionStatus.Pending),
            PaidTransactions = commissionsList.Count(c => c.Status == CommissionStatus.Paid)
        };
    }
}

public class GetPendingCommissionsForApprovalQueryHandler : IRequestHandler<GetPendingCommissionsForApprovalQuery, IEnumerable<CommissionTransactionDto>>
{
    private readonly ICommissionTransactionRepository _commissionRepository;

    public GetPendingCommissionsForApprovalQueryHandler(ICommissionTransactionRepository commissionRepository)
    {
        _commissionRepository = commissionRepository;
    }

    public async Task<IEnumerable<CommissionTransactionDto>> Handle(GetPendingCommissionsForApprovalQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.PageNumber - 1) * request.PageSize;
        var pendingCommissions = await _commissionRepository.GetPendingForApprovalAsync(
            skip,
            request.PageSize,
            cancellationToken);

        return pendingCommissions.Select(MapToDto);
    }

    private static CommissionTransactionDto MapToDto(Domain.Entities.CommissionTransaction commission)
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

public class GetCommissionsByDateRangeQueryHandler : IRequestHandler<GetCommissionsByDateRangeQuery, IEnumerable<CommissionTransactionDto>>
{
    private readonly ICommissionTransactionRepository _commissionRepository;

    public GetCommissionsByDateRangeQueryHandler(ICommissionTransactionRepository commissionRepository)
    {
        _commissionRepository = commissionRepository;
    }

    public async Task<IEnumerable<CommissionTransactionDto>> Handle(GetCommissionsByDateRangeQuery request, CancellationToken cancellationToken)
    {
        var commissions = await _commissionRepository.GetByDateRangeAsync(
            request.UserId ?? Guid.Empty,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        return commissions.Select(MapToDto);
    }

    private static CommissionTransactionDto MapToDto(Domain.Entities.CommissionTransaction commission)
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

public class GetAllCommissionsQueryHandler : IRequestHandler<GetAllCommissionsQuery, IEnumerable<CommissionTransactionDto>>
{
    private readonly ICommissionTransactionRepository _commissionRepository;

    public GetAllCommissionsQueryHandler(ICommissionTransactionRepository commissionRepository)
    {
        _commissionRepository = commissionRepository;
    }

    public async Task<IEnumerable<CommissionTransactionDto>> Handle(GetAllCommissionsQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.PageNumber - 1) * request.PageSize;
        var commissions = await _commissionRepository.GetAllAsync(skip, request.PageSize, cancellationToken);
        return commissions.Select(MapToDto);
    }

    private static CommissionTransactionDto MapToDto(Domain.Entities.CommissionTransaction commission)
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