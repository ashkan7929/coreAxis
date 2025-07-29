using CoreAxis.Modules.MLMModule.Domain.Entities;

namespace CoreAxis.Modules.MLMModule.Domain.Repositories;

public interface ICommissionRuleSetRepository
{
    Task<CommissionRuleSet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CommissionRuleSet?> GetDefaultAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<CommissionRuleSet?> GetByProductIdAsync(Guid productId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<CommissionRuleSet>> GetActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<CommissionRuleSet>> GetByTenantIdAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(CommissionRuleSet commissionRuleSet, CancellationToken cancellationToken = default);
    Task UpdateAsync(CommissionRuleSet commissionRuleSet, CancellationToken cancellationToken = default);
    Task DeleteAsync(CommissionRuleSet commissionRuleSet, CancellationToken cancellationToken = default);
}