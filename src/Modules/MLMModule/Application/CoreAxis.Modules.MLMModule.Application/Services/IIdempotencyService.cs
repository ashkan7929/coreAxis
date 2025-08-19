namespace CoreAxis.Modules.MLMModule.Application.Services;

public interface IIdempotencyService
{
    /// <summary>
    /// Checks if a payment has already been processed for commission calculation
    /// </summary>
    /// <param name="sourcePaymentId">The payment ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if already processed, false otherwise</returns>
    Task<bool> IsPaymentProcessedAsync(Guid sourcePaymentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a payment as processed for commission calculation
    /// </summary>
    /// <param name="sourcePaymentId">The payment ID to mark as processed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkPaymentAsProcessedAsync(Guid sourcePaymentId, CancellationToken cancellationToken = default);
}