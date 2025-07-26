using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using CoreAxis.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Repositories;

public class PageRepository : IPageRepository
{
    private readonly AuthDbContext _context;

    public PageRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<Page?> GetByIdAsync(Guid id)
    {
        return await _context.Pages.FindAsync(id);
    }

    public async Task<Page?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Pages
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<Page?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        return await _context.Pages
            .FirstOrDefaultAsync(p => p.Path == path, cancellationToken);
    }

    public async Task<IEnumerable<Page>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Pages
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Pages
            .AnyAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<bool> IsPathExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        return await _context.Pages
            .AnyAsync(p => p.Path == path, cancellationToken);
    }

    public async Task<IEnumerable<Page>> GetByModuleAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        return await _context.Pages
            .Where(p => p.ModuleName == moduleName)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Page page)
    {
        await _context.Pages.AddAsync(page);
    }

    public async Task UpdateAsync(Page page, CancellationToken cancellationToken = default)
    {
        _context.Pages.Update(page);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var page = await _context.Pages.FindAsync(id);
        if (page != null)
        {
            _context.Pages.Remove(page);
        }
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<Page>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Pages.ToListAsync(cancellationToken);
    }

    // IRepository<Page> interface implementations
    public IQueryable<Page> GetAll()
    {
        return _context.Pages.AsQueryable();
    }

    public IQueryable<Page> Find(Expression<Func<Page, bool>> predicate)
    {
        return _context.Pages.Where(predicate);
    }

    public void Update(Page entity)
    {
        _context.Pages.Update(entity);
    }

    public void Delete(Page entity)
    {
        _context.Pages.Remove(entity);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Pages.AnyAsync(p => p.Id == id);
    }

    public async Task<int> CountAsync(Expression<Func<Page, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return await _context.Pages.CountAsync();
        }
        return await _context.Pages.CountAsync(predicate);
    }
}