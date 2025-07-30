using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Repositories;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<Permission?> GetByPageAndActionAsync(Guid pageId, Guid actionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetByPageAsync(Guid pageId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetByActionAsync(Guid actionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid pageId, Guid actionId, CancellationToken cancellationToken = default);
    Task<bool> IsPermissionInUseAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default);
}