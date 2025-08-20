using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.Modules.MLMModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Repositories;

public class CommissionTransactionRepository : ICommissionTransactionRepository
{
    private readonly MLMModuleDbContext _context;

    public CommissionTransactionRepository(MLMModuleDbContext context)
    {
        _context = context;
    }

    public async Task<CommissionTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionTransactions
            .Include(ct => ct.UserReferral)
            .Include(ct => ct.CommissionRuleSet)
            .FirstOrDefaultAsync(ct => ct.Id == id, cancellationToken);
    }

    public async Task<List<CommissionTransaction>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionTransactions
            .Include(ct => ct.UserReferral)
            .Include(ct => ct.CommissionRuleSet)
            .Where(ct => ct.UserId == userId)
            .OrderByDescending(ct => ct.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CommissionTransaction>> GetByStatusAsync(CommissionStatus status, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionTransactions
            .Include(ct => ct.UserReferral)
            .Include(ct => ct.CommissionRuleSet)
            .Where(ct => ct.Status == status)
            .OrderByDescending(ct => ct.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CommissionTransaction>> GetBySourcePaymentIdAsync(Guid sourcePaymentId, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionTransactions
            .Include(ct => ct.UserReferral)
            .Include(ct => ct.CommissionRuleSet)
            .Where(ct => ct.SourcePaymentId == sourcePaymentId)
            .OrderBy(ct => ct.Level)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CommissionTransaction>> GetByDateRangeAsync(Guid userId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionTransactions
            .Include(ct => ct.UserReferral)
            .Include(ct => ct.CommissionRuleSet)
            .Where(ct => ct.UserId == userId && ct.CreatedOn >= fromDate && ct.CreatedOn <= toDate)
            .OrderByDescending(ct => ct.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalEarningsAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CommissionTransactions
            .Where(ct => ct.UserId == userId && ct.Status == CommissionStatus.Approved);

        if (fromDate.HasValue)
            query = query.Where(ct => ct.CreatedOn >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ct => ct.CreatedOn <= toDate.Value);

        return await query.SumAsync(ct => ct.Amount, cancellationToken);
    }

    public async Task<decimal> GetTotalPendingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionTransactions
            .Where(ct => ct.UserId == userId && ct.Status == CommissionStatus.Pending)
            .SumAsync(ct => ct.Amount, cancellationToken);
    }

    public async Task<List<CommissionTransaction>> GetPendingForApprovalAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionTransactions
            .Include(ct => ct.UserReferral)
            .Include(ct => ct.CommissionRuleSet)
            .Where(ct => ct.Status == CommissionStatus.Pending)
            .OrderBy(ct => ct.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionTransactions
            .AnyAsync(ct => ct.Id == id, cancellationToken);
    }

    public async Task AddAsync(CommissionTransaction commissionTransaction, CancellationToken cancellationToken = default)
    {
        await _context.CommissionTransactions.AddAsync(commissionTransaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CommissionTransaction commissionTransaction, CancellationToken cancellationToken = default)
    {
        _context.CommissionTransactions.Update(commissionTransaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CommissionTransaction commissionTransaction, CancellationToken cancellationToken = default)
    {
        _context.CommissionTransactions.Remove(commissionTransaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<CommissionTransaction>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.CommissionTransactions
            .Include(ct => ct.UserReferral)
            .Include(ct => ct.CommissionRuleSet)
            .OrderByDescending(ct => ct.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CommissionTransaction>> GetCommissionsAsync(Guid? userId, string? status, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.CommissionTransactions
            .Include(ct => ct.UserReferral)
            .Include(ct => ct.CommissionRuleSet)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(ct => ct.UserId == userId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CommissionStatus>(status, out var statusEnum))
            query = query.Where(ct => ct.Status == statusEnum);

        if (fromDate.HasValue)
            query = query.Where(ct => ct.CreatedOn >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ct => ct.CreatedOn <= toDate.Value);

        return await query
            .OrderByDescending(ct => ct.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CommissionTransaction>> GetUserCommissionsAsync(Guid userId, string? status, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.CommissionTransactions
            .Include(ct => ct.UserReferral)
            .Include(ct => ct.CommissionRuleSet)
            .Where(ct => ct.UserId == userId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CommissionStatus>(status, out var statusEnum))
            query = query.Where(ct => ct.Status == statusEnum);

        if (fromDate.HasValue)
            query = query.Where(ct => ct.CreatedOn >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ct => ct.CreatedOn <= toDate.Value);

        return await query
            .OrderByDescending(ct => ct.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}