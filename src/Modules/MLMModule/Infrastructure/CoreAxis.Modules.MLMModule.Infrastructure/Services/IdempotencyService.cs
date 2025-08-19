using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly ICommissionTransactionRepository _commissionTransactionRepository;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(
        ICommissionTransactionRepository commissionTransactionRepository,
        ILogger<IdempotencyService> logger)
    {
        _commissionTransactionRepository = commissionTransactionRepository;
        _logger = logger;
    }

    public async Task<bool> IsPaymentProcessedAsync(Guid sourcePaymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingTransactions = await _commissionTransactionRepository
                .GetBySourcePaymentIdAsync(sourcePaymentId, cancellationToken);
            
            var isProcessed = existingTransactions.Any();
            
            _logger.LogDebug("Payment {SourcePaymentId} processed check: {IsProcessed}", 
                sourcePaymentId, isProcessed);
            
            return isProcessed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if payment {SourcePaymentId} is processed", sourcePaymentId);
            throw;
        }
    }

    public async Task MarkPaymentAsProcessedAsync(Guid sourcePaymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // This method is called after commission transactions are created
            // The actual marking is done by creating CommissionTransaction records
            // So this method can be used for additional logging or validation if needed
            
            _logger.LogDebug("Payment {SourcePaymentId} marked as processed", sourcePaymentId);
            
            // In this implementation, the marking is implicit through the existence of CommissionTransaction records
            // If explicit marking is needed, we could create a separate ProcessedPayments table
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking payment {SourcePaymentId} as processed", sourcePaymentId);
            throw;
        }
    }
}