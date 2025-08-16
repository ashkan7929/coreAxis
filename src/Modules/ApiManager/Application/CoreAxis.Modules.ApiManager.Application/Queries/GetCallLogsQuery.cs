using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
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
    List<CallLogDto> CallLogs,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public record CallLogDto(
    Guid Id,
    Guid WebServiceId,
    string WebServiceName,
    Guid MethodId,
    string MethodName,
    string MethodPath,
    string HttpMethod,
    string? CorrelationId,
    bool Succeeded,
    int? StatusCode,
    long LatencyMs,
    string? Error,
    DateTime CreatedAt,
    string? RequestDump,
    string? ResponseDump
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
            query = query.Where(cl => cl.Succeeded == request.Succeeded.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(cl => cl.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(cl => cl.CreatedAt <= request.ToDate.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var callLogs = await query
            .OrderByDescending(cl => cl.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(cl => new CallLogDto(
                cl.Id,
                cl.WebServiceId,
                cl.WebService.Name,
                cl.MethodId,
                cl.Method.Path,
                cl.Method.Path,
                cl.Method.HttpMethod,
                cl.CorrelationId,
                cl.Succeeded,
                cl.StatusCode,
                cl.LatencyMs,
                cl.Error,
                cl.CreatedAt,
                cl.RequestDump,
                cl.ResponseDump
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} call logs (page {PageNumber}/{TotalPages})", 
            callLogs.Count, request.PageNumber, (int)Math.Ceiling((double)totalCount / request.PageSize));

        return new GetCallLogsResult(callLogs, totalCount, request.PageNumber, request.PageSize);
    }
}