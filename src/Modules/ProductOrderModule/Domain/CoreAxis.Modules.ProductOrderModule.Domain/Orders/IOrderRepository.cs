using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<List<Order>> GetUserOrdersAsync(Guid userId, int page = 1, int pageSize = 10);
    Task<int> GetUserOrdersCountAsync(Guid userId);
    Task<List<Order>> GetAllOrdersAsync(
        OrderStatus? status = null,
        string? assetCode = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<int> GetAllOrdersCountAsync(
        OrderStatus? status = null,
        string? assetCode = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Order order);
    Task<int> SaveChangesAsync();
}