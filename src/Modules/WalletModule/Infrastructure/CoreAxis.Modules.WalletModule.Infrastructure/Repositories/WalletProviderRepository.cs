using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Repositories;

public class WalletProviderRepository : IWalletProviderRepository
{
    private readonly WalletDbContext _context;

    public WalletProviderRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<WalletProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WalletProviders
            .FirstOrDefaultAsync(wp => wp.Id == id, cancellationToken);
    }

    public async Task<WalletProvider?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.WalletProviders
            .FirstOrDefaultAsync(wp => wp.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<WalletProvider>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        return await _context.WalletProviders
            .Where(wp => wp.Type == type)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WalletProvider>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WalletProviders
            .Where(wp => wp.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WalletProvider>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WalletProviders
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WalletProvider walletProvider, CancellationToken cancellationToken = default)
    {
        await _context.WalletProviders.AddAsync(walletProvider, cancellationToken);
    }

    public Task UpdateAsync(WalletProvider walletProvider, CancellationToken cancellationToken = default)
    {
        _context.WalletProviders.Update(walletProvider);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WalletProvider walletProvider, CancellationToken cancellationToken = default)
    {
        _context.WalletProviders.Remove(walletProvider);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.WalletProviders.AnyAsync(wp => wp.Name == name, cancellationToken);
    }
}