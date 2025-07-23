using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, Guid tenantId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> IsUsernameExistsAsync(string username, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> IsEmailExistsAsync(string email, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<User?> GetWithRolesAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<User?> GetWithPermissionsAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}