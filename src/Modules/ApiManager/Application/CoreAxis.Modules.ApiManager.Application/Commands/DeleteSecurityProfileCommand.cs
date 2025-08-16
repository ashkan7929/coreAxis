using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record DeleteSecurityProfileCommand(Guid Id) : IRequest<bool>;

public class DeleteSecurityProfileCommandHandler : IRequestHandler<DeleteSecurityProfileCommand, bool>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DeleteSecurityProfileCommandHandler> _logger;

    public DeleteSecurityProfileCommandHandler(DbContext dbContext, ILogger<DeleteSecurityProfileCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteSecurityProfileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogSensitiveOperation("Deleting security profile", new { ProfileId = request.Id });

        var profile = await _dbContext.Set<SecurityProfile>()
            .FirstOrDefaultAsync(sp => sp.Id == request.Id, cancellationToken);

        if (profile == null)
        {
            _logger.LogWarning("Security profile {ProfileId} not found for deletion", request.Id);
            return false;
        }

        // Check if profile is being used by any web services
        var isInUse = await _dbContext.Set<WebService>()
            .AnyAsync(ws => ws.SecurityProfileId == request.Id, cancellationToken);

        if (isInUse)
        {
            _logger.LogWarning("Cannot delete security profile {ProfileId} as it is in use by web services", request.Id);
            throw new InvalidOperationException("Cannot delete security profile that is in use by web services");
        }

        // Log configuration removal before deletion
        _logger.LogConfigurationAccess("Removing configuration", request.Id.ToString());
        
        _dbContext.Set<SecurityProfile>().Remove(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogSecurityProfileOperation("Deleted", request.Id.ToString(), $"Type: {profile.Type}");
        return true;
    }
}