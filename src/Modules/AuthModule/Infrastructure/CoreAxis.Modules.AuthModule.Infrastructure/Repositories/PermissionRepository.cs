using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly AuthDbContext _context;

    public PermissionRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(Guid id)
    {
        return await _context.Permissions.FindAsync(id);
    }

    public async Task<Permission?> GetByPageAndActionAsync(Guid pageId, Guid actionId, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .FirstOrDefaultAsync(p => p.PageId == pageId && p.ActionId == actionId, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByPageAsync(Guid pageId, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .Where(p => p.PageId == pageId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByActionAsync(Guid actionId, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .Where(p => p.ActionId == actionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetActivePermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .Where(p => p.IsActive)
            .Include(p => p.Page)
            .Include(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid pageId, Guid actionId, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .AnyAsync(p => p.PageId == pageId && p.ActionId == actionId, cancellationToken);
    }

    public async Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        await _context.Permissions.AddAsync(permission, cancellationToken);
    }

    public async Task UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        _context.Permissions.Update(permission);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        _context.Permissions.Remove(permission);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .Include(p => p.Page)
            .Include(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);
    }

    // IRepository<Permission> interface implementations
    public IQueryable<Permission> GetAll()
    {
        return _context.Permissions.AsQueryable();
    }

    public IQueryable<Permission> Find(Expression<Func<Permission, bool>> predicate)
    {
        return _context.Permissions.Where(predicate);
    }

    public async Task AddAsync(Permission entity)
    {
        await _context.Permissions.AddAsync(entity);
    }

    public void Update(Permission entity)
    {
        _context.Permissions.Update(entity);
    }

    public void Delete(Permission entity)
    {
        _context.Permissions.Remove(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission != null)
        {
            _context.Permissions.Remove(permission);
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Permissions.AnyAsync(p => p.Id == id);
    }

    public async Task<int> CountAsync(Expression<Func<Permission, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return await _context.Permissions.CountAsync();
        }
        return await _context.Permissions.CountAsync(predicate);
    }
}