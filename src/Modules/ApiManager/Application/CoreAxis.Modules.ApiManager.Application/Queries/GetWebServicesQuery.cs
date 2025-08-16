using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Queries;

public record GetWebServicesQuery(
    string? OwnerTenantId = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<GetWebServicesResult>;

public record GetWebServicesResult(
    List<WebServiceDto> WebServices,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public record WebServiceDto(
    Guid Id,
    string Name,
    string Description,
    string BaseUrl,
    bool IsActive,
    Guid? SecurityProfileId,
    string? SecurityProfileType,
    string? OwnerTenantId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int MethodCount
);

public class GetWebServicesQueryHandler : IRequestHandler<GetWebServicesQuery, GetWebServicesResult>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<GetWebServicesQueryHandler> _logger;

    public GetWebServicesQueryHandler(DbContext dbContext, ILogger<GetWebServicesQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<GetWebServicesResult> Handle(GetWebServicesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<WebService>()
            .Include(ws => ws.SecurityProfile)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.OwnerTenantId))
        {
            query = query.Where(ws => ws.OwnerTenantId == request.OwnerTenantId);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(ws => ws.IsActive == request.IsActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var webServices = await query
            .OrderBy(ws => ws.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(ws => new WebServiceDto(
                ws.Id,
                ws.Name,
                ws.Description,
                ws.BaseUrl,
                ws.IsActive,
                ws.SecurityProfileId,
                ws.SecurityProfile != null ? ws.SecurityProfile.Type.ToString() : null,
                ws.OwnerTenantId,
                ws.CreatedAt,
                ws.UpdatedAt,
                ws.Methods.Count(m => m.IsActive)
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} WebServices (page {PageNumber}/{TotalPages})", 
            webServices.Count, request.PageNumber, (int)Math.Ceiling((double)totalCount / request.PageSize));

        return new GetWebServicesResult(webServices, totalCount, request.PageNumber, request.PageSize);
    }
}