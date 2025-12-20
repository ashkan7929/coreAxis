using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ApiManager.Domain.Repositories;

public interface IWebServiceCallLogRepository : IRepository<WebServiceCallLog>
{
    Task<PaginatedList<WebServiceCallLog>> GetPagedAsync(
        Guid? webServiceId = null,
        Guid? methodId = null,
        bool? isSuccess = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<WebServiceCallLog>> GetByWebServiceIdAsync(Guid webServiceId, int? limit = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<WebServiceCallLog>> GetByMethodIdAsync(Guid methodId, int? limit = null, CancellationToken cancellationToken = default);
    Task<long> GetTotalCallsAsync(Guid? webServiceId = null, Guid? methodId = null, CancellationToken cancellationToken = default);
    Task<long> GetSuccessfulCallsAsync(Guid? webServiceId = null, Guid? methodId = null, CancellationToken cancellationToken = default);
    Task<double> GetAverageLatencyAsync(Guid? webServiceId = null, Guid? methodId = null, CancellationToken cancellationToken = default);
}
