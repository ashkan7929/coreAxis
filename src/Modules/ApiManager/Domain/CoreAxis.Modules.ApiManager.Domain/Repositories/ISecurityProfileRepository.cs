using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ApiManager.Domain.Repositories;

public interface ISecurityProfileRepository : IRepository<SecurityProfile>
{
    Task<IEnumerable<SecurityProfile>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> IsUsedByWebServicesAsync(Guid id, CancellationToken cancellationToken = default);
}
