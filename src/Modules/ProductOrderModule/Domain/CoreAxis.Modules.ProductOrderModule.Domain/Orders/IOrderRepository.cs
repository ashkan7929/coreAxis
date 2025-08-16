namespace CoreAxis.Modules.ProductOrderModule.Domain.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<List<Order>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 10);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Order order);
    Task<int> SaveChangesAsync();
}