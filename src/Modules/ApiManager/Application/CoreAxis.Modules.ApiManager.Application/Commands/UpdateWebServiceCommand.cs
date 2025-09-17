using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record UpdateWebServiceCommand(
    Guid Id,
    string Name,
    string BaseUrl,
    string? Description = null,
    Guid? SecurityProfileId = null
) : IRequest<bool>;

public class UpdateWebServiceCommandHandler : IRequestHandler<UpdateWebServiceCommand, bool>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<UpdateWebServiceCommandHandler> _logger;

    public UpdateWebServiceCommandHandler(DbContext dbContext, ILogger<UpdateWebServiceCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateWebServiceCommand request, CancellationToken cancellationToken)
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

        var webService = await _dbContext.Set<WebService>()
            .FirstOrDefaultAsync(ws => ws.Id == request.Id, cancellationToken);
        
        if (webService == null)
        {
            throw new ArgumentException($"WebService with ID {request.Id} not found");
        }

        // Check for duplicate name (excluding current service)
        var existingService = await _dbContext.Set<WebService>()
            .AnyAsync(ws => ws.Name == request.Name && ws.Id != request.Id, cancellationToken);
        
        if (existingService)
        {
            throw new InvalidOperationException($"WebService with name '{request.Name}' already exists");
        }

        webService.Update(request.Name, request.BaseUrl, request.Description, request.SecurityProfileId);
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated WebService {Name} with ID {Id}", request.Name, webService.Id);

        return true;
    }
}