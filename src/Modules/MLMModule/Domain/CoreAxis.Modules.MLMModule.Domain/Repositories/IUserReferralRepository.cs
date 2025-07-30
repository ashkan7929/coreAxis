using CoreAxis.Modules.MLMModule.Domain.Entities;

namespace CoreAxis.Modules.MLMModule.Domain.Repositories;

public interface IUserReferralRepository
{
    Task<UserReferral?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserReferral?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<UserReferral>> GetChildrenAsync(Guid parentUserId, CancellationToken cancellationToken = default);
    Task<List<UserReferral>> GetUplineAsync(Guid userId, int maxLevels = 10, CancellationToken cancellationToken = default);
    Task<List<UserReferral>> GetDownlineAsync(Guid userId, int maxLevels = 10, CancellationToken cancellationToken = default);
    Task<int> GetNetworkSizeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserReferral userReferral, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserReferral userReferral, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserReferral userReferral, CancellationToken cancellationToken = default);
    Task<List<UserReferral>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}