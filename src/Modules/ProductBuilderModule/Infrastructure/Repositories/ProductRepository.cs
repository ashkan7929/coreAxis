using CoreAxis.Modules.ProductBuilderModule.Domain.Entities;
using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.Modules.ProductBuilderModule.Infrastructure.Data;
using CoreAxis.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ProductBuilderModule.Infrastructure.Repositories;

public class ProductRepository : Repository<ProductDefinition>, IProductRepository
{
    private readonly ProductBuilderDbContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public ProductRepository(ProductBuilderDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<ProductDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.ProductDefinitions
            .FirstOrDefaultAsync(p => p.Key == key, cancellationToken);
    }

    public async Task<ProductVersion?> GetVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVersions
            .Include(v => v.Binding)
            .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);
    }

    public async Task<ProductVersion?> GetPublishedVersionAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVersions
            .Include(v => v.Binding)
            .Where(v => v.ProductId == productId && v.Status == SharedKernel.Versioning.VersionStatus.Published)
            .OrderByDescending(v => v.PublishedAt) // Get latest published
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductVersion?> GetVersionByNumberAsync(Guid productId, string versionNumber, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVersions
            .Include(v => v.Binding)
            .FirstOrDefaultAsync(v => v.ProductId == productId && v.VersionNumber == versionNumber, cancellationToken);
    }

    public async Task AddVersionAsync(ProductVersion version, CancellationToken cancellationToken = default)
    {
        await _context.ProductVersions.AddAsync(version, cancellationToken);
    }

    public Task UpdateVersionAsync(ProductVersion version, CancellationToken cancellationToken = default)
    {
        _context.ProductVersions.Update(version);
        return Task.CompletedTask;
    }
}
