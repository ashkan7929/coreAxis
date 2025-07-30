using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly WalletDbContext _context;

    public WalletRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .Include(w => w.WalletType)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<Wallet?> GetByUserAndTypeAsync(Guid userId, Guid walletTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .Include(w => w.WalletType)
            .FirstOrDefaultAsync(w => w.UserId == userId && w.WalletTypeId == walletTypeId, cancellationToken);
    }

    public async Task<IEnumerable<Wallet>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .Include(w => w.WalletType)
            .Where(w => w.UserId == userId)
            .ToListAsync(cancellationToken);
    }



    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        await _context.Wallets.AddAsync(wallet, cancellationToken);
    }

    public Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _context.Wallets.Update(wallet);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        _context.Wallets.Remove(wallet);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid walletTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets.AnyAsync(w => w.UserId == userId && w.WalletTypeId == walletTypeId, cancellationToken);
    }
}