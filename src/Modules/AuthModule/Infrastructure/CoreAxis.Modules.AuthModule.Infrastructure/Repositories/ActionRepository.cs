using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Repositories;

public class ActionRepository : IActionRepository
{
    private readonly AuthDbContext _context;

    public ActionRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<CoreAxis.Modules.AuthModule.Domain.Entities.Action?> GetByIdAsync(Guid id)
    {
        return await _context.Actions.FindAsync(id);
    }

    public async Task<CoreAxis.Modules.AuthModule.Domain.Entities.Action?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Actions
            .FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<CoreAxis.Modules.AuthModule.Domain.Entities.Action>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Actions
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Actions
            .AnyAsync(a => a.Code == code, cancellationToken);
    }

    public async Task AddAsync(CoreAxis.Modules.AuthModule.Domain.Entities.Action action)
    {
        await _context.Actions.AddAsync(action);
    }

    public async Task UpdateAsync(CoreAxis.Modules.AuthModule.Domain.Entities.Action action, CancellationToken cancellationToken = default)
    {
        _context.Actions.Update(action);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var action = await _context.Actions.FindAsync(id);
        if (action != null)
        {
            _context.Actions.Remove(action);
        }
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<CoreAxis.Modules.AuthModule.Domain.Entities.Action>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Actions.ToListAsync(cancellationToken);
    }

    // IRepository<Action> interface implementations
    public IQueryable<CoreAxis.Modules.AuthModule.Domain.Entities.Action> GetAll()
    {
        return _context.Actions.AsQueryable();
    }

    public IQueryable<CoreAxis.Modules.AuthModule.Domain.Entities.Action> Find(Expression<Func<CoreAxis.Modules.AuthModule.Domain.Entities.Action, bool>> expression)
    {
        return _context.Actions.Where(expression);
    }

    public void Update(CoreAxis.Modules.AuthModule.Domain.Entities.Action entity)
    {
        _context.Actions.Update(entity);
    }

    public void Delete(CoreAxis.Modules.AuthModule.Domain.Entities.Action entity)
    {
        _context.Actions.Remove(entity);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Actions.AnyAsync(a => a.Id == id);
    }

    public async Task<int> CountAsync(Expression<Func<CoreAxis.Modules.AuthModule.Domain.Entities.Action, bool>>? filter = null)
    {
        if (filter != null)
            return await _context.Actions.CountAsync(filter);
        return await _context.Actions.CountAsync();
    }
}