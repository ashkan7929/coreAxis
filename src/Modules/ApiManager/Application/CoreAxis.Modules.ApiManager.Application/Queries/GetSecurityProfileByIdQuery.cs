using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Queries;

public record GetSecurityProfileByIdQuery(Guid Id) : IRequest<SecurityProfileDto?>;

public class GetSecurityProfileByIdQueryHandler : IRequestHandler<GetSecurityProfileByIdQuery, SecurityProfileDto?>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<GetSecurityProfileByIdQueryHandler> _logger;

    public GetSecurityProfileByIdQueryHandler(DbContext dbContext, ILogger<GetSecurityProfileByIdQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<SecurityProfileDto?> Handle(GetSecurityProfileByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving security profile with ID {ProfileId}", request.Id);

        var profile = await _dbContext.Set<SecurityProfile>()
            .Where(sp => sp.Id == request.Id)
            .Select(sp => new SecurityProfileDto(
                sp.Id,
                sp.Type.ToString(),
                sp.ConfigJson,
                sp.RotationPolicy,
                sp.CreatedAt,
                sp.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
        {
            _logger.LogWarning("Security profile with ID {ProfileId} not found", request.Id);
        }
        else
        {
            _logger.LogInformation("Retrieved security profile {ProfileId}", request.Id);
        }

        return profile;
    }
}