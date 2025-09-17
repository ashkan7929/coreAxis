using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Queries;

public record GetCallLogsQuery(
    Guid? WebServiceId = null,
    Guid? MethodId = null,
    bool? Succeeded = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<GetCallLogsResult>;

public record GetCallLogsResult(
    List<WebServiceCallLogDto> CallLogs,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public class GetCallLogsQueryHandler : IRequestHandler<GetCallLogsQuery, GetCallLogsResult>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<GetCallLogsQueryHandler> _logger;

    public GetCallLogsQueryHandler(DbContext dbContext, ILogger<GetCallLogsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<GetCallLogsResult> Handle(GetCallLogsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving call logs with filters: WebServiceId={WebServiceId}, MethodId={MethodId}, Succeeded={Succeeded}, FromDate={FromDate}, ToDate={ToDate}, Page={PageNumber}, PageSize={PageSize}",
            request.WebServiceId, request.MethodId, request.Succeeded, request.FromDate, request.ToDate, request.PageNumber, request.PageSize);

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

        if (request.Succeeded.HasValue)
        {
            query = query.Where(cl => cl.IsSuccess == request.Succeeded.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(cl => cl.CalledAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(cl => cl.CalledAt <= request.ToDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var callLogs = await query
            .OrderByDescending(cl => cl.CalledAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(cl => new WebServiceCallLogDto(
                cl.Id,
                cl.WebServiceId,
                cl.MethodId,
                cl.RequestBody,
                cl.ResponseBody,
                cl.StatusCode,
                cl.IsSuccess,
                cl.ErrorMessage,
                cl.LatencyMs,
                cl.CalledAt,
                cl.WebService.Name,
                cl.Method != null ? $"{cl.Method.HttpMethod} {cl.Method.Path}" : null
            ))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        _logger.LogInformation("Retrieved {Count} call logs out of {TotalCount} total", callLogs.Count, totalCount);

        return new GetCallLogsResult(
            callLogs,
            totalCount,
            request.PageNumber,
            request.PageSize,
            totalPages
        );
    }
}