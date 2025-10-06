using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetByCodeAsync(string code);
    Task<List<Product>> GetAllAsync(
        ProductStatus? status = null,
        string? q = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<int> GetAllCountAsync(
        ProductStatus? status = null,
        string? q = null,
        CancellationToken cancellationToken = default);
    Task<List<ProductListItem>> GetLightweightAsync(
        ProductStatus? status = null,
        string? q = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<int> GetLightweightCountAsync(
        ProductStatus? status = null,
        string? q = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
    Task<int> SaveChangesAsync();
}