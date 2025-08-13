using CoreAxis.Modules.WalletModule.Domain.Entities;

namespace CoreAxis.Modules.WalletModule.Domain.Repositories;

public interface IWalletTypeRepository
{
    Task<WalletType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WalletType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletType>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(WalletType walletType, CancellationToken cancellationToken = default);
    Task UpdateAsync(WalletType walletType, CancellationToken cancellationToken = default);
    Task DeleteAsync(WalletType walletType, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IWalletProviderRepository
{
    Task<WalletProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WalletProvider?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletProvider>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletProvider>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletProvider>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
    Task AddAsync(WalletProvider provider, CancellationToken cancellationToken = default);
    Task UpdateAsync(WalletProvider provider, CancellationToken cancellationToken = default);
    Task DeleteAsync(WalletProvider provider, CancellationToken cancellationToken = default);
}

public interface IWalletContractRepository
{
    Task<WalletContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WalletContract?> GetByUserAndProviderAsync(Guid userId, Guid providerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletContract>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletContract>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletContract>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task AddAsync(WalletContract contract, CancellationToken cancellationToken = default);
    Task UpdateAsync(WalletContract contract, CancellationToken cancellationToken = default);
    Task DeleteAsync(WalletContract contract, CancellationToken cancellationToken = default);
}

public interface ITransactionTypeRepository
{
    Task<TransactionType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TransactionType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionType>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TransactionType transactionType, CancellationToken cancellationToken = default);
    Task UpdateAsync(TransactionType transactionType, CancellationToken cancellationToken = default);
    Task DeleteAsync(TransactionType transactionType, CancellationToken cancellationToken = default);
}