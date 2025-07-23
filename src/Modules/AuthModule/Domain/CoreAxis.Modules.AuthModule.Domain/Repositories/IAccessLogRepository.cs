using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Repositories;

public interface IAccessLogRepository : IRepository<AccessLog>
{
    Task<IEnumerable<AccessLog>> GetByUserAsync(Guid userId, Guid tenantId, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccessLog>> GetByTenantAsync(Guid tenantId, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccessLog>> GetFailedLoginsAsync(Guid tenantId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccessLog>> GetByActionAsync(string action, Guid tenantId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task<int> GetFailedLoginCountAsync(string username, Guid tenantId, DateTime fromDate, CancellationToken cancellationToken = default);
}