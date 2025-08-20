using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.MLMModule.Application.Services;

public class CommissionManagementService : ICommissionManagementService
{
    private readonly ICommissionTransactionRepository _commissionRepository;
    private readonly ILogger<CommissionManagementService> _logger;

    public CommissionManagementService(
        ICommissionTransactionRepository commissionRepository,
        ILogger<CommissionManagementService> logger)
    {
        _commissionRepository = commissionRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<CommissionTransactionDto>>> GetCommissionsAsync(
        GetCommissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commissions = await _commissionRepository.GetCommissionsAsync(
                request.UserId,
                request.Status,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var commissionDtos = commissions.Select(MapToDto);
            return Result<IEnumerable<CommissionTransactionDto>>.Success(commissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commissions with request: {@Request}", request);
            return Result<IEnumerable<CommissionTransactionDto>>.Failure("Failed to get commissions");
        }
    }

    public async Task<Result<bool>> ApproveCommissionAsync(
        Guid commissionId,
        Guid approvedBy,
        ApproveCommissionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commission = await _commissionRepository.GetByIdAsync(commissionId, cancellationToken);
            if (commission == null)
            {
                return Result<bool>.Failure("Commission not found");
            }

            if (commission.Status != CommissionStatus.Pending)
            {
                return Result<bool>.Failure("Commission is not in pending status");
            }

            commission.Approve(approvedBy.ToString(), request.Notes);
            await _commissionRepository.UpdateAsync(commission, cancellationToken);

            _logger.LogInformation("Commission {CommissionId} approved by {ApprovedBy}", commissionId, approvedBy);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving commission {CommissionId}", commissionId);
            return Result<bool>.Failure("Failed to approve commission");
        }
    }

    public async Task<Result<bool>> RejectCommissionAsync(
        Guid commissionId,
        Guid rejectedBy,
        RejectCommissionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commission = await _commissionRepository.GetByIdAsync(commissionId, cancellationToken);
            if (commission == null)
            {
                return Result<bool>.Failure("Commission not found");
            }

            if (commission.Status != CommissionStatus.Pending)
            {
                return Result<bool>.Failure("Commission is not in pending status");
            }

            commission.Reject(rejectedBy.ToString(), request.Reason, request.Notes);
            await _commissionRepository.UpdateAsync(commission, cancellationToken);

            _logger.LogInformation("Commission {CommissionId} rejected by {RejectedBy}", commissionId, rejectedBy);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting commission {CommissionId}", commissionId);
            return Result<bool>.Failure("Failed to reject commission");
        }
    }

    public async Task<Result<CommissionTransactionDto>> GetCommissionByIdAsync(
        Guid commissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commission = await _commissionRepository.GetByIdAsync(commissionId, cancellationToken);
            if (commission == null)
            {
                return Result<CommissionTransactionDto>.Failure("Commission not found");
            }

            var dto = MapToDto(commission);
            return Result<CommissionTransactionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commission {CommissionId}", commissionId);
            return Result<CommissionTransactionDto>.Failure("Failed to get commission");
        }
    }

    public async Task<Result<IEnumerable<CommissionTransactionDto>>> GetUserCommissionsAsync(
        Guid userId,
        GetCommissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commissions = await _commissionRepository.GetUserCommissionsAsync(
                userId,
                request.Status,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var commissionDtos = commissions.Select(MapToDto);
            return Result<IEnumerable<CommissionTransactionDto>>.Success(commissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user commissions for user {UserId}", userId);
            return Result<IEnumerable<CommissionTransactionDto>>.Failure("Failed to get user commissions");
        }
    }

    private static CommissionTransactionDto MapToDto(CommissionTransaction commission)
    {
        return new CommissionTransactionDto
        {
            Id = commission.Id,
            UserId = commission.UserId,
            SourcePaymentId = commission.SourcePaymentId,
            Level = commission.Level,
            Amount = commission.Amount,
            Percentage = commission.Percentage,
            SourceAmount = commission.SourceAmount,
            Status = commission.Status,
            ApprovedBy = commission.ApprovedBy,
            ApprovedAt = commission.ApprovedAt,
            ApprovalNotes = commission.ApprovalNotes,
            RejectedBy = commission.RejectedBy,
            RejectedAt = commission.RejectedAt,
            RejectionReason = commission.RejectionReason,
            RejectionNotes = commission.RejectionNotes,
            CreatedOn = commission.CreatedOn,
            LastModifiedOn = commission.LastModifiedOn
        };
    }
}