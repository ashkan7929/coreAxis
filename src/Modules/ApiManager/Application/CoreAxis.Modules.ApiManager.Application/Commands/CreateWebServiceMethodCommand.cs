using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record CreateWebServiceMethodCommand(
    Guid WebServiceId,
    string Name,
    string Description,
    string Path,
    string HttpMethod,
    int TimeoutMs = 30000,
    string? RetryPolicyJson = null,
    string? CircuitPolicyJson = null,
    List<CreateWebServiceParamDto>? Parameters = null
) : IRequest<Guid>;

public record CreateWebServiceParamDto(
    string Name,
    string Description,
    ParameterLocation Location,
    string DataType,
    bool IsRequired = false,
    string? DefaultValue = null
);

public class CreateWebServiceMethodCommandHandler : IRequestHandler<CreateWebServiceMethodCommand, Guid>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<CreateWebServiceMethodCommandHandler> _logger;

    public CreateWebServiceMethodCommandHandler(DbContext dbContext, ILogger<CreateWebServiceMethodCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateWebServiceMethodCommand request, CancellationToken cancellationToken)
    {
        // Validate web service exists
        var webServiceExists = await _dbContext.Set<WebService>()
            .AnyAsync(ws => ws.Id == request.WebServiceId && ws.IsActive, cancellationToken);
        
        if (!webServiceExists)
        {
            throw new ArgumentException($"WebService with ID {request.WebServiceId} not found or inactive");
        }

        // Check for duplicate method (same WebServiceId, Path, and HttpMethod)
        var existingMethod = await _dbContext.Set<WebServiceMethod>()
            .AnyAsync(m => m.WebServiceId == request.WebServiceId && 
                          m.Path == request.Path && 
                          m.HttpMethod == request.HttpMethod, cancellationToken);
        
        if (existingMethod)
        {
            throw new InvalidOperationException(
                $"Method with path '{request.Path}' and HTTP method '{request.HttpMethod}' already exists for this WebService");
        }

        var method = new WebServiceMethod(
            request.WebServiceId,
            request.Path,
            request.HttpMethod,
            request.TimeoutMs,
            retryPolicyJson: request.RetryPolicyJson,
            circuitPolicyJson: request.CircuitPolicyJson
        );

        _dbContext.Set<WebServiceMethod>().Add(method);

        // Add parameters if provided
        if (request.Parameters?.Any() == true)
        {
            foreach (var paramDto in request.Parameters)
            {
                var parameter = new WebServiceParam(
                    method.Id,
                    paramDto.Name,
                    paramDto.Location,
                    paramDto.DataType,
                    paramDto.IsRequired,
                    paramDto.DefaultValue
                );
                
                _dbContext.Set<WebServiceParam>().Add(parameter);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created WebServiceMethod {Name} with ID {Id} for WebService {WebServiceId}", 
            request.Name, method.Id, request.WebServiceId);

        return method.Id;
    }
}