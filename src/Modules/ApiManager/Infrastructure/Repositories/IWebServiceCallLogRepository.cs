using CoreAxis.Modules.ApiManager.Domain.Entities;
using CoreAxis.Shared.Abstractions.Repositories;
using CoreAxis.Shared.Abstractions.Pagination;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Repositories;

public interface IWebServiceCallLogRepository : IRepository<WebServiceCallLog>
{
    Task<PagedResult<WebServiceCallLog>> GetPagedAsync(
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