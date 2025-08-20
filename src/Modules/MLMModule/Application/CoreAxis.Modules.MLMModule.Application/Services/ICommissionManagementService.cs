using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.MLMModule.Application.Services;

public interface ICommissionManagementService
{
    /// <summary>
    /// Gets commissions with filtering and pagination
    /// </summary>
    /// <param name="request">Filter and pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of commissions</returns>
    Task<Result<IEnumerable<CommissionTransactionDto>>> GetCommissionsAsync(
        GetCommissionsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a commission
    /// </summary>
    /// <param name="commissionId">Commission ID</param>
    /// <param name="approvedBy">User ID who approved</param>
    /// <param name="request">Approval request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> ApproveCommissionAsync(
        Guid commissionId,
        Guid approvedBy,
        ApproveCommissionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a commission
    /// </summary>
    /// <param name="commissionId">Commission ID</param>
    /// <param name="rejectedBy">User ID who rejected</param>
    /// <param name="request">Rejection request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> RejectCommissionAsync(
        Guid commissionId,
        Guid rejectedBy,
        RejectCommissionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets commission by ID
    /// </summary>
    /// <param name="commissionId">Commission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Commission details</returns>
    Task<Result<CommissionTransactionDto>> GetCommissionByIdAsync(
        Guid commissionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user commissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Filter and pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's commissions</returns>
    Task<Result<IEnumerable<CommissionTransactionDto>>> GetUserCommissionsAsync(
        Guid userId,
        GetCommissionsRequest request,
        CancellationToken cancellationToken = default);
}