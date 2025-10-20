using CoreAxis.Modules.ProductOrderModule.Domain.Entities;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Suppliers;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id);
    Task<Supplier?> GetByCodeAsync(string code);
    Task<Supplier?> GetByNameAsync(string name);
    Task<List<Supplier>> GetAllAsync(
        bool? isActive = null,
        string? q = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<int> GetAllCountAsync(
        bool? isActive = null,
        string? q = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Supplier supplier);
    Task<int> SaveChangesAsync();
}