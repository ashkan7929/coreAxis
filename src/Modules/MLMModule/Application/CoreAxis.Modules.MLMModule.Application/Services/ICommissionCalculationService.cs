using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.MLMModule.Application.Services;

public interface ICommissionCalculationService
{
    /// <summary>
    /// Processes a PaymentConfirmed event and generates commission transactions
    /// </summary>
    /// <param name="sourcePaymentId">The payment ID that triggered the commission</param>
    /// <param name="productId">The product ID associated with the payment</param>
    /// <param name="amount">The payment amount</param>
    /// <param name="buyerUserId">The user who made the payment</param>
    /// <param name="correlationId">Correlation ID for idempotency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated commission transactions</returns>
    Task<Result<List<CommissionTransactionDto>>> ProcessPaymentConfirmedAsync(
        Guid sourcePaymentId,
        Guid productId,
        decimal amount,
        Guid buyerUserId,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates potential commissions without creating transactions (for preview)
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="amount">The payment amount</param>
    /// <param name="buyerUserId">The user who would make the payment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of potential commission calculations</returns>
    Task<Result<List<CommissionCalculationDto>>> CalculatePotentialCommissionsAsync(
        Guid productId,
        decimal amount,
        Guid buyerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a payment can generate commissions
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="amount">The payment amount</param>
    /// <param name="buyerUserId">The user who made the payment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<Result<bool>> ValidateCommissionEligibilityAsync(
        Guid productId,
        decimal amount,
        Guid buyerUserId,
        CancellationToken cancellationToken = default);
}