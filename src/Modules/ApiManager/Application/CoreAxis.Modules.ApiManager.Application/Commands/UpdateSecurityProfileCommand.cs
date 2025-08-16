using MediatR;
using Microsoft.Extensions.Logging;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Infrastructure;
using CoreAxis.Modules.ApiManager.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record UpdateSecurityProfileCommand(
    Guid Id,
    string? ConfigJson,
    string? RotationPolicy
) : IRequest<bool>;

public class UpdateSecurityProfileCommandHandler : IRequestHandler<UpdateSecurityProfileCommand, bool>
{
    private readonly ApiManagerDbContext _context;
    private readonly ILogger<UpdateSecurityProfileCommandHandler> _logger;

    public UpdateSecurityProfileCommandHandler(
        ApiManagerDbContext context,
        ILogger<UpdateSecurityProfileCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateSecurityProfileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogSensitiveOperation("Updating security profile", new { ProfileId = request.Id, HasConfigUpdate = !string.IsNullOrEmpty(request.ConfigJson) });

        var profile = await _context.Set<SecurityProfile>()
            .FirstOrDefaultAsync(sp => sp.Id == request.Id, cancellationToken);

        if (profile == null)
        {
            _logger.LogWarning("Security profile with ID {ProfileId} not found for update", request.Id);
            return false;
        }

        // Update only provided fields
        var configJson = !string.IsNullOrEmpty(request.ConfigJson) ? request.ConfigJson : profile.ConfigJson;
        var rotationPolicy = !string.IsNullOrEmpty(request.RotationPolicy) ? request.RotationPolicy : profile.RotationPolicy;

        profile.Update(
            profile.Type,
            configJson,
            rotationPolicy
        );

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogSecurityProfileOperation("Updated", request.Id.ToString(), $"Type: {profile.Type}");
        if (!string.IsNullOrEmpty(request.ConfigJson))
        {
            _logger.LogConfigurationAccess("Updated configuration", request.Id.ToString());
        }
        
        return true;
    }
}