using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Queries;

public record GetWebServiceByIdQuery(Guid Id) : IRequest<WebServiceDto?>;

public class GetWebServiceByIdQueryHandler : IRequestHandler<GetWebServiceByIdQuery, WebServiceDto?>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<GetWebServiceByIdQueryHandler> _logger;

    public GetWebServiceByIdQueryHandler(DbContext dbContext, ILogger<GetWebServiceByIdQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<WebServiceDto?> Handle(GetWebServiceByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving WebService with ID {Id}", request.Id);

        var webService = await _dbContext.Set<WebService>()
            .Include(ws => ws.SecurityProfile)
            .Include(ws => ws.Methods.Where(m => m.IsActive))
            .ThenInclude(m => m.Parameters)
            .FirstOrDefaultAsync(ws => ws.Id == request.Id, cancellationToken);

        if (webService == null)
        {
            _logger.LogWarning("WebService with ID {Id} not found", request.Id);
            return null;
        }

        var dto = new WebServiceDto(
            webService.Id,
            webService.Name,
            webService.BaseUrl,
            webService.Description,
            webService.SecurityProfileId,
            webService.IsActive,
            webService.OwnerTenantId,
            webService.CreatedAt,
            webService.UpdatedAt,
            webService.SecurityProfile != null ? new SecurityProfileSummaryDto(
                webService.SecurityProfile.Id,
                webService.SecurityProfile.Type.ToString(),
                webService.SecurityProfile.ConfigJson,
                webService.SecurityProfile.RotationPolicy
            ) : null,
            webService.Methods.Select(m => new WebServiceMethodSummaryDto(
                        m.Id,
                        m.Path,
                        m.HttpMethod,
                        m.TimeoutMs,
                        m.IsActive
                    )).ToList()
        );

        _logger.LogInformation("Retrieved WebService {Name} with {MethodCount} methods", 
            webService.Name, webService.Methods.Count);

        return dto;
    }
}