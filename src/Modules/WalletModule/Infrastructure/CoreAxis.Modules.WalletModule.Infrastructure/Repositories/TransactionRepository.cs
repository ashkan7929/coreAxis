using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly WalletDbContext _context;

    public TransactionRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.TransactionType)
            .Include(t => t.Wallet)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.TransactionType)
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.TransactionType)
            .Include(t => t.Wallet)
            .Where(t => t.Wallet.UserId == userId)
            .OrderByDescending(t => t.CreatedOn)
            .ToListAsync(cancellationToken);
    }



    public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Include(t => t.TransactionType)
            .Include(t => t.Wallet)
            .Where(t => t.CreatedOn >= fromDate && t.CreatedOn <= toDate);

        if (userId.HasValue)
        {
            query = query.Where(t => t.Wallet.UserId == userId.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByTypeAsync(Guid transactionTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.TransactionType)
            .Include(t => t.Wallet)
            .Where(t => t.TransactionTypeId == transactionTypeId)
            .OrderByDescending(t => t.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalAmountByWalletAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.WalletId == walletId && t.Status == TransactionStatus.Completed)
            .SumAsync(t => t.Amount, cancellationToken);
    }

    public async Task<int> GetTransactionCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }

    public Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Update(transaction);
        return Task.CompletedTask;
    }
}