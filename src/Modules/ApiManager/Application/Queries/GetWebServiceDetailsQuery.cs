using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Queries;

public record GetWebServiceDetailsQuery(Guid Id) : IRequest<WebServiceDetailsDto?>;

public class GetWebServiceDetailsQueryHandler : IRequestHandler<GetWebServiceDetailsQuery, WebServiceDetailsDto?>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<GetWebServiceDetailsQueryHandler> _logger;

    public GetWebServiceDetailsQueryHandler(DbContext dbContext, ILogger<GetWebServiceDetailsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<WebServiceDetailsDto?> Handle(GetWebServiceDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving web service details for ID {WebServiceId}", request.Id);

        var webService = await _dbContext.Set<WebService>()
            .Include(ws => ws.SecurityProfile)
            .Include(ws => ws.Methods.Where(m => m.IsActive))
                .ThenInclude(m => m.Parameters)
            .Where(ws => ws.Id == request.Id)
            .Select(ws => new WebServiceDetailsDto(
                ws.Id,
                ws.Name,
                ws.Description,
                ws.BaseUrl,
                ws.IsActive,
                ws.OwnerTenantId,
                ws.CreatedAt,
                ws.UpdatedAt,
                ws.SecurityProfile != null ? new SecurityProfileDto(
                    ws.SecurityProfile.Id,
                    ws.SecurityProfile.Type.ToString(),
                    ws.SecurityProfile.ConfigJson,
                    ws.SecurityProfile.RotationPolicy,
                    ws.SecurityProfile.CreatedAt,
                    ws.SecurityProfile.UpdatedAt
                ) : null,
                ws.Methods.Select(m => new WebServiceMethodDto(
                    m.Id,
                    m.Name,
                    m.Description,
                    m.Path,
                    m.HttpMethod,
                    m.TimeoutMs,
                    m.RetryPolicyJson,
                    m.CircuitPolicyJson,
                    m.IsActive,
                    m.CreatedAt,
                    m.UpdatedAt,
                    m.Parameters.Select(p => new WebServiceParamDto(
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Location,
                        p.DataType,
                        p.IsRequired,
                        p.DefaultValue,
                        p.CreatedAt,
                        p.UpdatedAt
                    )).ToList()
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (webService == null)
        {
            _logger.LogWarning("Web service with ID {WebServiceId} not found", request.Id);
        }
        else
        {
            _logger.LogInformation("Retrieved web service details for {WebServiceName} (ID: {WebServiceId})", 
                webService.Name, request.Id);
        }

        return webService;
    }
}