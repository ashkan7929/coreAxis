using CoreAxis.Modules.ApiManager.Domain.Entities;
using CoreAxis.Shared.Abstractions.Repositories;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Repositories;

public interface ISecurityProfileRepository : IRepository<SecurityProfile>
{
    Task<IEnumerable<SecurityProfile>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsUsedByWebServicesAsync(Guid id, CancellationToken cancellationToken = default);
}