using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Queries;

public record GetWebServiceMethodsQuery(
    Guid WebServiceId,
    bool? IsActive = null,
    string? HttpMethod = null
) : IRequest<List<WebServiceMethodDto>>;

public class GetWebServiceMethodsQueryHandler : IRequestHandler<GetWebServiceMethodsQuery, List<WebServiceMethodDto>>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<GetWebServiceMethodsQueryHandler> _logger;

    public GetWebServiceMethodsQueryHandler(DbContext dbContext, ILogger<GetWebServiceMethodsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<WebServiceMethodDto>> Handle(GetWebServiceMethodsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving methods for WebService {WebServiceId}", request.WebServiceId);

        var query = _dbContext.Set<WebServiceMethod>()
            .Include(m => m.Parameters)
            .Where(m => m.WebServiceId == request.WebServiceId);

        // Apply filters
        if (request.IsActive.HasValue)
        {
            query = query.Where(m => m.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrEmpty(request.HttpMethod))
        {
            query = query.Where(m => m.HttpMethod == request.HttpMethod.ToUpperInvariant());
        }

        var methods = await query
            .OrderBy(m => m.Path)
            .ThenBy(m => m.HttpMethod)
            .Select(m => new WebServiceMethodDto(
                 m.Id,
                 m.WebServiceId,
                 m.Path,
                 m.HttpMethod,
                 m.RequestSchema,
                 m.ResponseSchema,
                 m.TimeoutMs,
                 m.RetryPolicyJson,
                 m.CircuitPolicyJson,
                 m.IsActive,
                 m.CreatedAt,
                 m.UpdatedAt,
                 m.Parameters.Select(p => new WebServiceParamDto(
                      p.Id,
                      p.MethodId,
                      p.Name,
                      p.Location,
                      p.Type,
                      p.IsRequired,
                      p.DefaultValue,
                      p.CreatedAt,
                      p.UpdatedAt
                  )).ToList()
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} methods for WebService {WebServiceId}", 
            methods.Count, request.WebServiceId);

        return methods;
    }
}