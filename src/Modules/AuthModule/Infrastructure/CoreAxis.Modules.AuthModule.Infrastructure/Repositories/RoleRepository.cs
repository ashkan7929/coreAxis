using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using CoreAxis.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AuthDbContext _context;

    public RoleRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        return await _context.Roles.FindAsync(id);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<bool> IsNameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AnyAsync(r => r.Name == name, cancellationToken);
    }



    public async Task<Role?> GetWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
     {
         return await _context.Roles
             .Include(r => r.RolePermissions)
                 .ThenInclude(rp => rp.Permission)
             .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
     }

    public async Task<IEnumerable<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Where(r => r.IsSystemRole)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RolePermission>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRolePermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
     {
         await _context.RolePermissions.AddAsync(rolePermission, cancellationToken);
     }

     public async Task AddRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var rolePermission = new RolePermission(roleId, permissionId, Guid.Empty); // Using Guid.Empty for assignedBy as placeholder
        
        await _context.RolePermissions.AddAsync(rolePermission, cancellationToken);
    }

    public async Task RemoveRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
        
        if (rolePermission != null)
        {
            _context.RolePermissions.Remove(rolePermission);
        }
    }

    public async Task AddPermissionToRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var rolePermission = new RolePermission(roleId, permissionId, Guid.Empty);
        await _context.RolePermissions.AddAsync(rolePermission, cancellationToken);
    }

    public async Task RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
        
        if (rolePermission != null)
        {
            _context.RolePermissions.Remove(rolePermission);
        }
    }

    public async Task<IEnumerable<User>> GetUsersByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Include(ur => ur.User)
            .Select(ur => ur.User)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveAllRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var rolePermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);
        
        _context.RolePermissions.RemoveRange(rolePermissions);
    }

    public async Task UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        // Remove existing permissions
        await RemoveAllRolePermissionsAsync(roleId, cancellationToken);
        
        // Add new permissions
        foreach (var permissionId in permissionIds)
        {
            await AddPermissionToRoleAsync(roleId, permissionId, cancellationToken);
        }
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Roles.Update(role);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Roles.Remove(role);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles.ToListAsync(cancellationToken);
    }

    // IRepository<Role> interface implementations
    public IQueryable<Role> GetAll()
    {
        return _context.Roles.AsQueryable();
    }

    public IQueryable<Role> Find(Expression<Func<Role, bool>> predicate)
    {
        return _context.Roles.Where(predicate);
    }

    public async Task AddAsync(Role entity)
    {
        await _context.Roles.AddAsync(entity);
    }

    public void Update(Role entity)
    {
        _context.Roles.Update(entity);
    }

    public void Delete(Role entity)
    {
        _context.Roles.Remove(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role != null)
        {
            _context.Roles.Remove(role);
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Roles.AnyAsync(r => r.Id == id);
    }

    public async Task<int> CountAsync(Expression<Func<Role, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return await _context.Roles.CountAsync();
        }
        return await _context.Roles.CountAsync(predicate);
    }
}