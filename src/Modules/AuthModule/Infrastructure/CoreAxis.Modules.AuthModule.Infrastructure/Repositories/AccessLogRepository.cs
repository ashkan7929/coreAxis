using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Repositories;

public class AccessLogRepository : IAccessLogRepository
{
    private readonly AuthDbContext _context;

    public AccessLogRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<AccessLog?> GetByIdAsync(Guid id)
    {
        return await _context.AccessLogs.FindAsync(id);
    }

    public async Task<IEnumerable<AccessLog>> GetByUserAsync(Guid userId, Guid tenantId, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await _context.AccessLogs
            .Where(al => al.UserId == userId && al.TenantId == tenantId)
            .OrderByDescending(al => al.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccessLog>> GetByTenantAsync(Guid tenantId, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await _context.AccessLogs
            .Where(al => al.TenantId == tenantId)
            .OrderByDescending(al => al.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccessLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, Guid? tenantId = null, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var query = _context.AccessLogs
            .Where(al => al.AccessTime >= startDate && al.AccessTime <= endDate);

        if (tenantId.HasValue)
        {
            query = query.Where(al => al.TenantId == tenantId.Value);
        }

        return await query
            .OrderByDescending(al => al.AccessTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AccessLog>> GetFailedLoginsAsync(Guid tenantId, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AccessLogs
            .Where(al => !al.IsSuccess && al.TenantId == tenantId);

        if (fromDate.HasValue)
        {
            query = query.Where(al => al.Timestamp >= fromDate.Value);
        }

        return await query
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetFailedLoginCountAsync(string username, Guid tenantId, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        return await _context.AccessLogs
            .CountAsync(al => al.Username == username && al.TenantId == tenantId && !al.IsSuccess && al.Timestamp >= fromDate, cancellationToken);
    }

    public async Task AddAsync(AccessLog accessLog)
    {
        await _context.AccessLogs.AddAsync(accessLog);
    }

    public async Task UpdateAsync(AccessLog accessLog, CancellationToken cancellationToken = default)
    {
        _context.AccessLogs.Update(accessLog);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var accessLog = await _context.AccessLogs.FindAsync(id);
        if (accessLog != null)
        {
            _context.AccessLogs.Remove(accessLog);
        }
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<AccessLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AccessLogs
            .OrderByDescending(al => al.AccessTime)
            .ToListAsync(cancellationToken);
    }

    public IQueryable<AccessLog> GetAll()
    {
        return _context.AccessLogs.AsQueryable();
    }

    public IQueryable<AccessLog> Find(Expression<Func<AccessLog, bool>> expression)
    {
        return _context.AccessLogs.Where(expression);
    }

    public void Update(AccessLog entity)
    {
        _context.AccessLogs.Update(entity);
    }

    public void Delete(AccessLog entity)
    {
        _context.AccessLogs.Remove(entity);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.AccessLogs.AnyAsync(al => al.Id == id);
    }

    public async Task<int> CountAsync(Expression<Func<AccessLog, bool>>? filter = null)
    {
        if (filter != null)
            return await _context.AccessLogs.CountAsync(filter);
        return await _context.AccessLogs.CountAsync();
    }

    public async Task<IEnumerable<AccessLog>> GetByActionAsync(string action, Guid tenantId, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AccessLogs
            .Where(al => al.Action == action && al.TenantId == tenantId);

        if (fromDate.HasValue)
        {
            query = query.Where(al => al.Timestamp >= fromDate.Value);
        }

        return await query
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync(cancellationToken);
    }
}