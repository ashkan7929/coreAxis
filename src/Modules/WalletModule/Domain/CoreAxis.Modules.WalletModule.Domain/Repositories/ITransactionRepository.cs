using CoreAxis.Modules.WalletModule.Domain.Entities;

namespace CoreAxis.Modules.WalletModule.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByTypeAsync(Guid transactionTypeId, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalAmountByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<int> GetTransactionCountAsync(CancellationToken cancellationToken = default);
    
    // Idempotency support
    Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}