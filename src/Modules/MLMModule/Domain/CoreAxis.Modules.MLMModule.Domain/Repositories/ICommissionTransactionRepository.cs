using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Enums;

namespace CoreAxis.Modules.MLMModule.Domain.Repositories;

public interface ICommissionTransactionRepository
{
    Task<CommissionTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetByUserIdAsync(Guid userId, Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetByStatusAsync(CommissionStatus status, Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetBySourcePaymentIdAsync(Guid sourcePaymentId, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetByDateRangeAsync(Guid userId, Guid tenantId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalEarningsAsync(Guid userId, Guid tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPendingAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetPendingForApprovalAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(CommissionTransaction commissionTransaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(CommissionTransaction commissionTransaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(CommissionTransaction commissionTransaction, CancellationToken cancellationToken = default);
    Task<List<CommissionTransaction>> GetByTenantIdAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}