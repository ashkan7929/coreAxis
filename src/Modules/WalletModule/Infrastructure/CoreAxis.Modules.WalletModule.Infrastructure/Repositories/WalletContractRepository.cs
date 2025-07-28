using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Repositories;

public class WalletContractRepository : IWalletContractRepository
{
    private readonly WalletDbContext _context;

    public WalletContractRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<WalletContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WalletContracts
            .Include(wc => wc.Wallet)
            .Include(wc => wc.Provider)
            .FirstOrDefaultAsync(wc => wc.Id == id, cancellationToken);
    }

    public async Task<WalletContract?> GetByUserAndProviderAsync(Guid userId, Guid providerId, CancellationToken cancellationToken = default)
    {
        return await _context.WalletContracts
            .Include(wc => wc.Wallet)
            .Include(wc => wc.Provider)
            .FirstOrDefaultAsync(wc => wc.UserId == userId && wc.ProviderId == providerId, cancellationToken);
    }

    public async Task<IEnumerable<WalletContract>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.WalletContracts
            .Include(wc => wc.Wallet)
            .Include(wc => wc.Provider)
            .Where(wc => wc.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WalletContract>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _context.WalletContracts
            .Include(wc => wc.Wallet)
            .Include(wc => wc.Provider)
            .Where(wc => wc.WalletId == walletId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WalletContract>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        return await _context.WalletContracts
            .Include(wc => wc.Wallet)
            .Include(wc => wc.Provider)
            .Where(wc => wc.ProviderId == providerId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WalletContract contract, CancellationToken cancellationToken = default)
    {
        await _context.WalletContracts.AddAsync(contract, cancellationToken);
    }

    public Task UpdateAsync(WalletContract contract, CancellationToken cancellationToken = default)
    {
        _context.WalletContracts.Update(contract);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WalletContract contract, CancellationToken cancellationToken = default)
    {
        _context.WalletContracts.Remove(contract);
        return Task.CompletedTask;
    }
}