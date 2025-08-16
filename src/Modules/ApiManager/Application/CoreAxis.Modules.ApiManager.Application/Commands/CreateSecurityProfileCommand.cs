using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CoreAxis.Modules.ApiManager.Infrastructure.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record CreateSecurityProfileCommand(
    string Type,
    string ConfigJson,
    string? RotationPolicy = null
) : IRequest<Guid>;

public class CreateSecurityProfileCommandHandler : IRequestHandler<CreateSecurityProfileCommand, Guid>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<CreateSecurityProfileCommandHandler> _logger;

    public CreateSecurityProfileCommandHandler(DbContext dbContext, ILogger<CreateSecurityProfileCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateSecurityProfileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogSensitiveOperation("Creating security profile", new { Type = request.Type, HasConfig = !string.IsNullOrEmpty(request.ConfigJson) });

        // Parse the security type
        if (!Enum.TryParse<SecurityType>(request.Type, true, out var securityType))
        {
            _logger.LogError("Invalid security type provided: {Type}", request.Type);
            throw new ArgumentException($"Invalid security type: {request.Type}");
        }

        var profile = new SecurityProfile(
            securityType,
            request.ConfigJson,
            request.RotationPolicy
        );

        _dbContext.Set<SecurityProfile>().Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogSecurityProfileOperation("Created", profile.Id.ToString(), $"Type: {request.Type}");
        _logger.LogConfigurationAccess("Stored configuration", profile.Id.ToString());

        return profile.Id;
    }
}