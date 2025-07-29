using CoreAxis.Modules.MLMModule.Domain.Entities;

namespace CoreAxis.Modules.MLMModule.Domain.Repositories;

public interface IUserReferralRepository
{
    Task<UserReferral?> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<UserReferral?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<UserReferral>> GetChildrenAsync(Guid parentUserId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<UserReferral>> GetUplineAsync(Guid userId, Guid tenantId, int maxLevels = 10, CancellationToken cancellationToken = default);
    Task<List<UserReferral>> GetDownlineAsync(Guid userId, Guid tenantId, int maxLevels = 10, CancellationToken cancellationToken = default);
    Task<int> GetNetworkSizeAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(UserReferral userReferral, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserReferral userReferral, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserReferral userReferral, CancellationToken cancellationToken = default);
    Task<List<UserReferral>> GetByTenantIdAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}