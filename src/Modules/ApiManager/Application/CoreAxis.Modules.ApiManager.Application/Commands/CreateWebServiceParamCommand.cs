using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record CreateWebServiceParamCommand(
    Guid MethodId,
    string Name,
    ParameterLocation Location,
    string Type,
    bool IsRequired = false,
    string? DefaultValue = null
) : IRequest<Guid>;

public class CreateWebServiceParamCommandHandler : IRequestHandler<CreateWebServiceParamCommand, Guid>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<CreateWebServiceParamCommandHandler> _logger;

    public CreateWebServiceParamCommandHandler(DbContext dbContext, ILogger<CreateWebServiceParamCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateWebServiceParamCommand request, CancellationToken cancellationToken)
    {
        // Validate method exists
        var methodExists = await _dbContext.Set<WebServiceMethod>()
            .AnyAsync(m => m.Id == request.MethodId, cancellationToken);
        
        if (!methodExists)
        {
            throw new ArgumentException($"WebServiceMethod with ID {request.MethodId} not found");
        }

        // Check for duplicate parameter name within the same method
        var existingParam = await _dbContext.Set<WebServiceParam>()
            .AnyAsync(p => p.MethodId == request.MethodId && p.Name == request.Name, cancellationToken);
        
        if (existingParam)
        {
            throw new InvalidOperationException($"Parameter with name '{request.Name}' already exists for this method");
        }

        var parameter = new WebServiceParam(
            request.MethodId,
            request.Name,
            request.Location,
            request.Type,
            request.IsRequired,
            request.DefaultValue
        );

        _dbContext.Set<WebServiceParam>().Add(parameter);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created WebServiceParam {Name} for method {MethodId} with ID {Id}", 
            request.Name, request.MethodId, parameter.Id);

        return parameter.Id;
    }
}