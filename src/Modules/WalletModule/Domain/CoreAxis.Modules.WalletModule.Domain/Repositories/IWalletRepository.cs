using CoreAxis.Modules.WalletModule.Domain.Entities;

namespace CoreAxis.Modules.WalletModule.Domain.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByUserAndTypeAsync(Guid userId, Guid walletTypeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Wallet>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);
    Task UpdateAsync(Wallet wallet, CancellationToken cancellationToken = default);
    Task DeleteAsync(Wallet wallet, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid walletTypeId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}