using CoreAxis.Modules.ApiManager.Domain.Entities;
using CoreAxis.Shared.Abstractions.Repositories;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Repositories;

public interface IWebServiceRepository : IRepository<WebService>
{
    Task<WebService?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WebService>> GetAllWithSecurityProfileAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByBaseUrlAsync(string baseUrl, Guid? excludeId = null, CancellationToken cancellationToken = default);
}