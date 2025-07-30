using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Enums;

namespace CoreAxis.Modules.MLMModule.Domain.Repositories;

public interface ICommissionTransactionRepository
{
    Task<CommissionTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetByStatusAsync(CommissionStatus status, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetBySourcePaymentIdAsync(Guid sourcePaymentId, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetByDateRangeAsync(Guid userId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalEarningsAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPendingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetPendingForApprovalAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(CommissionTransaction commissionTransaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(CommissionTransaction commissionTransaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(CommissionTransaction commissionTransaction, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}