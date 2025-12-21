using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.Modules.ApiManager.Infrastructure;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Repositories;

public class WebServiceCallLogRepository : Repository<WebServiceCallLog>, IWebServiceCallLogRepository
{
    public WebServiceCallLogRepository(ApiManagerDbContext context) : base(context)
    {
    }

    public async Task<PaginatedList<WebServiceCallLog>> GetPagedAsync(
        Guid? webServiceId = null,
        Guid? methodId = null,
        bool? isSuccess = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(log => log.WebService)
            .Include(log => log.Method)
            .AsQueryable();

        if (webServiceId.HasValue)
        {
            query = query.Where(log => log.WebServiceId == webServiceId.Value);
        }

        if (methodId.HasValue)
        {
            query = query.Where(log => log.MethodId == methodId.Value);
        }

        if (isSuccess.HasValue)
        {
            query = query.Where(log => log.Succeeded == isSuccess.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.CreatedAt <= toDate.Value);
        }

        query = query.OrderByDescending(log => log.CreatedAt);

        return await PaginatedList<WebServiceCallLog>.CreateAsync(query, pageNumber, pageSize, cancellationToken);
    }

    public async Task<IEnumerable<WebServiceCallLog>> GetByWebServiceIdAsync(Guid webServiceId, int? limit = null, CancellationToken cancellationToken = default)
    {
        IQueryable<WebServiceCallLog> query = _dbSet
            .Include(log => log.Method)
            .Where(log => log.WebServiceId == webServiceId)
            .OrderByDescending(log => log.CreatedAt);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WebServiceCallLog>> GetByMethodIdAsync(Guid methodId, int? limit = null, CancellationToken cancellationToken = default)
    {
        IQueryable<WebServiceCallLog> query = _dbSet
            .Include(log => log.WebService)
            .Where(log => log.MethodId == methodId)
            .OrderByDescending(log => log.CreatedAt);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<long> GetTotalCallsAsync(Guid? webServiceId = null, Guid? methodId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (webServiceId.HasValue)
        {
            query = query.Where(log => log.WebServiceId == webServiceId.Value);
        }

        if (methodId.HasValue)
        {
            query = query.Where(log => log.MethodId == methodId.Value);
        }

        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<long> GetSuccessfulCallsAsync(Guid? webServiceId = null, Guid? methodId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(log => log.Succeeded);

        if (webServiceId.HasValue)
        {
            query = query.Where(log => log.WebServiceId == webServiceId.Value);
        }

        if (methodId.HasValue)
        {
            query = query.Where(log => log.MethodId == methodId.Value);
        }

        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<double> GetAverageLatencyAsync(Guid? webServiceId = null, Guid? methodId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (webServiceId.HasValue)
        {
            query = query.Where(log => log.WebServiceId == webServiceId.Value);
        }

        if (methodId.HasValue)
        {
            query = query.Where(log => log.MethodId == methodId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            return await query.AverageAsync(log => log.LatencyMs, cancellationToken);
        }

        return 0;
    }
}
