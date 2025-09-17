using CoreAxis.Modules.ApiManager.Domain.Entities;
using CoreAxis.Shared.Abstractions.Repositories;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Repositories;

public interface IWebServiceMethodRepository : IRepository<WebServiceMethod>
{
    Task<WebServiceMethod?> GetByIdWithParametersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WebServiceMethod>> GetByWebServiceIdAsync(Guid webServiceId, bool? isActive = null, string? httpMethod = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAndWebServiceAsync(string name, Guid webServiceId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPathAndWebServiceAsync(string path, Guid webServiceId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}