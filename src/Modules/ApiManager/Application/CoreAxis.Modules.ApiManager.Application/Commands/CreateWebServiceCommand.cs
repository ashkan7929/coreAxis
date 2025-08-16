using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record CreateWebServiceCommand(
    string Name,
    string Description,
    string BaseUrl,
    Guid? SecurityProfileId = null,
    string? OwnerTenantId = null
) : IRequest<Guid>;

public class CreateWebServiceCommandHandler : IRequestHandler<CreateWebServiceCommand, Guid>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<CreateWebServiceCommandHandler> _logger;

    public CreateWebServiceCommandHandler(DbContext dbContext, ILogger<CreateWebServiceCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateWebServiceCommand request, CancellationToken cancellationToken)
    {
        // Validate security profile if provided
        if (request.SecurityProfileId.HasValue)
        {
            var securityProfileExists = await _dbContext.Set<SecurityProfile>()
                .AnyAsync(sp => sp.Id == request.SecurityProfileId.Value, cancellationToken);
            
            if (!securityProfileExists)
            {
                throw new ArgumentException($"Security profile with ID {request.SecurityProfileId} not found");
            }
        }

        // Check for duplicate name
        var existingService = await _dbContext.Set<WebService>()
            .AnyAsync(ws => ws.Name == request.Name, cancellationToken);
        
        if (existingService)
        {
            throw new InvalidOperationException($"WebService with name '{request.Name}' already exists");
        }

        var webService = new WebService(
            request.Name,
            request.Description,
            request.BaseUrl,
            request.SecurityProfileId,
            request.OwnerTenantId
        );

        _dbContext.Set<WebService>().Add(webService);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created WebService {Name} with ID {Id}", request.Name, webService.Id);

        return webService.Id;
    }
}