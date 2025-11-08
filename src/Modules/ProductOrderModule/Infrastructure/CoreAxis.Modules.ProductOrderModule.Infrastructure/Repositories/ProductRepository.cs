using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.Products;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductOrderDbContext _context;

    public ProductRepository(ProductOrderDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Set<Product>().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetByCodeAsync(string code)
    {
        return await _context.Set<Product>().FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<List<Product>> GetAllAsync(
        ProductStatus? status = null,
        string? q = null,
        Guid? supplierId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Product>().AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Code.Contains(term));
        }

        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierId == supplierId.Value);

        query = query.OrderBy(p => p.Name).ThenBy(p => p.CreatedOn);

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAllCountAsync(
        ProductStatus? status = null,
        string? q = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Product>().AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Code.Contains(term));
        }

        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierId == supplierId.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<ProductListItem>> GetLightweightAsync(
        ProductStatus? status = null,
        string? q = null,
        Guid? supplierId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Product>().AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Code.Contains(term));
        }

        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierId == supplierId.Value);

        query = query.OrderBy(p => p.Name).ThenBy(p => p.CreatedOn);

        return await query
            .Select(p => new ProductListItem
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Status = p.Status,
                Count = p.Count,
                Quantity = p.Quantity,
                PriceFrom = p.PriceFrom,
                Attributes = p.Attributes
            })
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetLightweightCountAsync(
        ProductStatus? status = null,
        string? q = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Product>().AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Code.Contains(term));
        }

        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierId == supplierId.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Product product)
    {
        await _context.Set<Product>().AddAsync(product);
    }

    public Task UpdateAsync(Product product)
    {
        _context.Set<Product>().Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Product product)
    {
        _context.Set<Product>().Remove(product);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}