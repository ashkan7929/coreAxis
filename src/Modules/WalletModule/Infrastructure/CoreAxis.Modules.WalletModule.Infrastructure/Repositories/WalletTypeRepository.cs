using CoreAxis.Modules.WalletModule.Domain.Entities;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Repositories;

public class WalletTypeRepository : IWalletTypeRepository
{
    private readonly WalletDbContext _context;

    public WalletTypeRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<WalletType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WalletTypes
            .FirstOrDefaultAsync(wt => wt.Id == id, cancellationToken);
    }

    public async Task<WalletType?> GetByNameAsync(string name, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.WalletTypes
            .FirstOrDefaultAsync(wt => wt.Name == name && wt.TenantId == tenantId, cancellationToken);
    }

    public async Task<IEnumerable<WalletType>> GetActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.WalletTypes
            .Where(wt => wt.IsActive && wt.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WalletType>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.WalletTypes
            .Where(wt => wt.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WalletType walletType, CancellationToken cancellationToken = default)
    {
        await _context.WalletTypes.AddAsync(walletType, cancellationToken);
    }

    public Task UpdateAsync(WalletType walletType, CancellationToken cancellationToken = default)
    {
        _context.WalletTypes.Update(walletType);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WalletType walletType, CancellationToken cancellationToken = default)
    {
        _context.WalletTypes.Remove(walletType);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string name, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.WalletTypes
            .AnyAsync(wt => wt.Name == name && wt.TenantId == tenantId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}