using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ProductOrderDbContext _context;

    public OrderRepository(ProductOrderDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return null;
            
        return await _context.Orders
            .Include(o => o.OrderLines)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);
    }

    public async Task<List<Order>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 10)
    {
        return await _context.Orders
            .Include(o => o.OrderLines)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
    }

    public async Task DeleteAsync(Order order)
    {
        _context.Orders.Remove(order);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}