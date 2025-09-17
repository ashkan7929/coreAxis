using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Queries;

public record GetWebServiceCallLogsQuery(
    Guid? WebServiceId = null,
    Guid? MethodId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool? IsSuccess = null,
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<GetWebServiceCallLogsResult>;

public record GetWebServiceCallLogsResult(
    List<WebServiceCallLogDto> CallLogs,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public class GetWebServiceCallLogsQueryHandler : IRequestHandler<GetWebServiceCallLogsQuery, GetWebServiceCallLogsResult>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<GetWebServiceCallLogsQueryHandler> _logger;

    public GetWebServiceCallLogsQueryHandler(DbContext dbContext, ILogger<GetWebServiceCallLogsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<GetWebServiceCallLogsResult> Handle(GetWebServiceCallLogsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving call logs with filters: WebServiceId={WebServiceId}, MethodId={MethodId}", 
            request.WebServiceId, request.MethodId);

        var query = _dbContext.Set<WebServiceCallLog>()
            .Include(cl => cl.WebService)
            .Include(cl => cl.Method)
            .AsQueryable();

        // Apply filters
        if (request.WebServiceId.HasValue)
        {
            query = query.Where(cl => cl.WebServiceId == request.WebServiceId.Value);
        }

        if (request.MethodId.HasValue)
        {
            query = query.Where(cl => cl.MethodId == request.MethodId.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(cl => cl.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(cl => cl.CreatedAt <= request.ToDate.Value);
        }

        if (request.IsSuccess.HasValue)
        {
            query = query.Where(cl => cl.Succeeded == request.IsSuccess.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var callLogs = await query
            .OrderByDescending(cl => cl.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(cl => new WebServiceCallLogDto(
                cl.Id,
                cl.WebServiceId,
                cl.MethodId,
                cl.CorrelationId,
                cl.RequestDump,
                cl.ResponseDump,
                cl.StatusCode,
                cl.LatencyMs,
                cl.Succeeded,
                cl.Error,
                cl.CreatedAt,
                cl.WebService.Name,
                cl.Method != null ? cl.Method.Path : "Unknown",
                cl.Method != null ? cl.Method.HttpMethod : "Unknown"
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} call logs (page {PageNumber}/{TotalPages})", 
            callLogs.Count, request.PageNumber, (int)Math.Ceiling((double)totalCount / request.PageSize));

        return new GetWebServiceCallLogsResult(callLogs, totalCount, request.PageNumber, request.PageSize);
    }
}