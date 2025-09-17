using CoreAxis.Modules.CommerceModule.Domain.Entities;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdWithDetailsAsync(Guid id);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<(List<Order> Orders, int TotalCount)> GetOrdersAsync(
        Guid? userId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<List<Order>> GetOrdersByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 10);
    Task<Order> AddAsync(Order order);
    Task<Order> UpdateAsync(Order order);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> OrderNumberExistsAsync(string orderNumber, Guid? excludeId = null);
    Task<string> GenerateOrderNumberAsync();
    Task<List<Order>> GetOrdersByStatusAsync(string status);
    Task<decimal> GetTotalOrderValueByUserIdAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null);
}

public interface IOrderItemRepository
{
    Task<OrderItem?> GetByIdAsync(Guid id);
    Task<List<OrderItem>> GetItemsByOrderIdAsync(Guid orderId);
    Task<OrderItem> AddAsync(OrderItem orderItem);
    Task<OrderItem> UpdateAsync(OrderItem orderItem);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<List<OrderItem>> GetItemsByInventoryItemIdAsync(Guid inventoryItemId);
}