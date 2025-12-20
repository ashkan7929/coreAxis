using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.ApiManager.Domain.Entities;
using CoreAxis.Modules.ApiManager.Infrastructure.Repositories;
using CoreAxis.Shared.Infrastructure.Repositories;
using CoreAxis.Shared.Abstractions.Pagination;

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
        var query = DbSet
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
            query = query.Where(log => log.IsSuccess == isSuccess.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.CalledAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.CalledAt <= toDate.Value);
        }

        query = query.OrderByDescending(log => log.CalledAt);

        // Use PaginatedList.CreateAsync instead of manually creating PagedResult
        return await PaginatedList<WebServiceCallLog>.CreateAsync(query, pageNumber, pageSize);
    }

    public async Task<IEnumerable<WebServiceCallLog>> GetByWebServiceIdAsync(Guid webServiceId, int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(log => log.Method)
            .Where(log => log.WebServiceId == webServiceId)
            .OrderByDescending(log => log.CalledAt);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WebServiceCallLog>> GetByMethodIdAsync(Guid methodId, int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(log => log.WebService)
            .Where(log => log.MethodId == methodId)
            .OrderByDescending(log => log.CalledAt);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<long> GetTotalCallsAsync(Guid? webServiceId = null, Guid? methodId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (webServiceId.HasValue)
        {
            query = query.Where(log => log.WebServiceId == webServiceId.Value);
        }

        if (methodId.HasValue)
        {
            query = query.Where(log => log.MethodId == methodId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<long> GetSuccessfulCallsAsync(Guid? webServiceId = null, Guid? methodId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(log => log.IsSuccess);

        if (webServiceId.HasValue)
        {
            query = query.Where(log => log.WebServiceId == webServiceId.Value);
        }

        if (methodId.HasValue)
        {
            query = query.Where(log => log.MethodId == methodId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<double> GetAverageLatencyAsync(Guid? webServiceId = null, Guid? methodId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (webServiceId.HasValue)
        {
            query = query.Where(log => log.WebServiceId == webServiceId.Value);
        }

        if (methodId.HasValue)
        {
            query = query.Where(log => log.MethodId == methodId.Value);
        }

        return await query.AverageAsync(log => (double)log.LatencyMs, cancellationToken);
    }
}