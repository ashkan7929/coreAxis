using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    public async Task<List<Order>> GetUserOrdersAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        return await _context.Orders
            .Include(o => o.OrderLines)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUserOrdersCountAsync(Guid userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .CountAsync();
    }

    public async Task<List<Order>> GetAllOrdersAsync(
        OrderStatus? status = null,
        string? assetCode = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.OrderLines)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(assetCode))
        {
            query = query.Where(o => o.AssetCode.Value == assetCode);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreatedOn <= toDate.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAllOrdersCountAsync(
        OrderStatus? status = null,
        string? assetCode = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(assetCode))
        {
            query = query.Where(o => o.AssetCode.Value == assetCode);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreatedOn <= toDate.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Order order)
    {
        _context.Orders.Remove(order);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync()
    {
        if (_context.Database.CurrentTransaction != null)
            await _context.Database.RollbackTransactionAsync();
    }

}