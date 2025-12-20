using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ApiManager.Domain.Repositories;

public interface IWebServiceRepository : IRepository<WebService>
{
    Task<WebService?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WebService>> GetAllWithSecurityProfileAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<WebService?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByBaseUrlAsync(string baseUrl, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
