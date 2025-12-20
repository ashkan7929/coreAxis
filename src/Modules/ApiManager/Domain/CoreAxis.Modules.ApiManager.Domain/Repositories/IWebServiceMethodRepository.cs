using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ApiManager.Domain.Repositories;

public interface IWebServiceMethodRepository : IRepository<WebServiceMethod>
{
    Task<WebServiceMethod?> GetByIdWithParametersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WebServiceMethod>> GetByWebServiceIdAsync(Guid webServiceId, bool? isActive = null, string? httpMethod = null, CancellationToken cancellationToken = default);
    Task<WebServiceMethod?> GetByServiceAndPathAsync(Guid webServiceId, string path, string httpMethod, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPathAndWebServiceAsync(string path, Guid webServiceId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
