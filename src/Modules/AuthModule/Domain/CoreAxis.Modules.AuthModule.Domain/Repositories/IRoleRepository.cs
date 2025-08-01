using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Repositories;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> IsNameExistsAsync(string name, CancellationToken cancellationToken = default);

    Task<Role?> GetWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<RolePermission>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task AddRolePermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);
    Task RemoveRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task AddPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task RemoveAllRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds, CancellationToken cancellationToken = default);
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
}