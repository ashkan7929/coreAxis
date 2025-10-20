using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Suppliers;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly ProductOrderDbContext _context;

    public SupplierRepository(ProductOrderDbContext context)
    {
        _context = context;
    }

    public async Task<Supplier?> GetByIdAsync(Guid id)
    {
        return await _context.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Supplier?> GetByCodeAsync(string code)
    {
        return await _context.Set<Supplier>().FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<Supplier?> GetByNameAsync(string name)
    {
        return await _context.Set<Supplier>().FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<List<Supplier>> GetAllAsync(
        bool? isActive = null,
        string? q = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Supplier>().AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s => s.Name.Contains(term) || s.Code.Contains(term));
        }

        query = query.OrderBy(s => s.Name).ThenBy(s => s.CreatedOn);

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAllCountAsync(
        bool? isActive = null,
        string? q = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Supplier>().AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s => s.Name.Contains(term) || s.Code.Contains(term));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Supplier supplier)
    {
        await _context.Set<Supplier>().AddAsync(supplier);
    }

    public Task UpdateAsync(Supplier supplier)
    {
        _context.Set<Supplier>().Update(supplier);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Supplier supplier)
    {
        // Soft delete to preserve audit and relations
        supplier.IsActive = false;
        _context.Set<Supplier>().Update(supplier);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}