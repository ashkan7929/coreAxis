using CoreAxis.Modules.ProductBuilderModule.Domain.Entities;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;

public interface IProductRepository : IRepository<ProductDefinition>
{
    Task<ProductDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<ProductVersion?> GetVersionAsync(Guid versionId, CancellationToken cancellationToken = default);
    Task AddVersionAsync(ProductVersion version, CancellationToken cancellationToken = default);
    Task UpdateVersionAsync(ProductVersion version, CancellationToken cancellationToken = default);
}
