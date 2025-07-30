using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Repositories;

public interface IAccessLogRepository : IRepository<AccessLog>
{
    Task<IEnumerable<AccessLog>> GetByUserAsync(Guid userId, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default);

    Task<IEnumerable<AccessLog>> GetFailedLoginsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccessLog>> GetByActionAsync(string action, DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task<int> GetFailedLoginCountAsync(string username, DateTime fromDate, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}