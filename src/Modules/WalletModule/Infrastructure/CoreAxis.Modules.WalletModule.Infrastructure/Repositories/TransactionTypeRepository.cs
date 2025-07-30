using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Repositories;

public class TransactionTypeRepository : ITransactionTypeRepository
{
    private readonly WalletDbContext _context;

    public TransactionTypeRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TransactionTypes
            .FirstOrDefaultAsync(tt => tt.Id == id, cancellationToken);
    }

    public async Task<TransactionType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.TransactionTypes
            .FirstOrDefaultAsync(tt => tt.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<TransactionType>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TransactionTypes
            .Where(tt => tt.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TransactionType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TransactionTypes
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TransactionType transactionType, CancellationToken cancellationToken = default)
    {
        await _context.TransactionTypes.AddAsync(transactionType, cancellationToken);
    }

    public Task UpdateAsync(TransactionType transactionType, CancellationToken cancellationToken = default)
    {
        _context.TransactionTypes.Update(transactionType);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TransactionType transactionType, CancellationToken cancellationToken = default)
    {
        _context.TransactionTypes.Remove(transactionType);
        return Task.CompletedTask;
    }
}