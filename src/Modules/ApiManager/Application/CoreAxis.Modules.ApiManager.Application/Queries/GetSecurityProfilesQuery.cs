using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Queries;

public record GetSecurityProfilesQuery() : IRequest<List<SecurityProfileDto>>;

public class GetSecurityProfilesQueryHandler : IRequestHandler<GetSecurityProfilesQuery, List<SecurityProfileDto>>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<GetSecurityProfilesQueryHandler> _logger;

    public GetSecurityProfilesQueryHandler(DbContext dbContext, ILogger<GetSecurityProfilesQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<SecurityProfileDto>> Handle(GetSecurityProfilesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all security profiles");

        var profiles = await _dbContext.Set<SecurityProfile>()
            .Select(sp => new SecurityProfileDto(
                sp.Id,
                sp.Type.ToString(),
                sp.ConfigJson,
                sp.RotationPolicy,
                sp.CreatedAt,
                sp.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} security profiles", profiles.Count);
        return profiles;
    }
}