using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Queries;

public record GetWebServiceDetailsQuery(Guid WebServiceId) : IRequest<WebServiceDetailsDto?>;

public record WebServiceDetailsDto(
    Guid Id,
    string Name,
    string Description,
    string BaseUrl,
    bool IsActive,
    Guid? SecurityProfileId,
    SecurityProfileSummaryDto? SecurityProfile,
    string? OwnerTenantId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<WebServiceMethodDto> Methods
);



public record WebServiceMethodDto(
    Guid Id,
    string Name,
    string Description,
    string Path,
    string HttpMethod,
    bool IsActive,
    int TimeoutMs,
    string? RetryPolicyJson,
    string? CircuitPolicyJson,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<WebServiceParamDto> Parameters
);

public record WebServiceParamDto(
    Guid Id,
    string Name,
    string Description,
    string Location,
    string DataType,
    bool IsRequired,
    string? DefaultValue
);

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
        var webService = await _dbContext.Set<WebService>()
            .Include(ws => ws.SecurityProfile)
            .Include(ws => ws.Methods.Where(m => m.IsActive))
                .ThenInclude(m => m.Parameters)
            .FirstOrDefaultAsync(ws => ws.Id == request.WebServiceId, cancellationToken);

        if (webService == null)
        {
            _logger.LogWarning("WebService with ID {WebServiceId} not found", request.WebServiceId);
            return null;
        }

        var result = new WebServiceDetailsDto(
            webService.Id,
            webService.Name,
            webService.Description,
            webService.BaseUrl,
            webService.IsActive,
            webService.SecurityProfileId,
            webService.SecurityProfile != null ? new SecurityProfileSummaryDto(
                webService.SecurityProfile.Id,
                webService.SecurityProfile.Type.ToString(),
                webService.SecurityProfile.ConfigJson,
                webService.SecurityProfile.RotationPolicy
            ) : null,
            webService.OwnerTenantId,
            webService.CreatedAt,
            webService.UpdatedAt,
            webService.Methods.Select(m => new WebServiceMethodDto(
                m.Id,
                m.Path, // Name field
                "", // Description not available
                m.Path,
                m.HttpMethod,
                m.IsActive,
                m.TimeoutMs,
                m.RetryPolicyJson,
                m.CircuitPolicyJson,
                m.CreatedAt,
                m.UpdatedAt,
                m.Parameters.Select(p => new WebServiceParamDto(
                    p.Id,
                    p.Name,
                    "", // Description not available in domain
                    p.Location.ToString(),
                    p.Type,
                    p.IsRequired,
                    p.DefaultValue
                )).ToList()
            )).ToList()
        );

        _logger.LogInformation("Retrieved WebService details for {Name} with {MethodCount} methods", 
            webService.Name, webService.Methods.Count);

        return result;
    }
}