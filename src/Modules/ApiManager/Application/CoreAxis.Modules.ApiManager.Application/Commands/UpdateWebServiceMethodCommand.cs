using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record UpdateWebServiceMethodCommand(
    Guid Id,
    string Path,
    string HttpMethod,
    int TimeoutMs = 30000,
    string? RequestSchema = null,
    string? ResponseSchema = null,
    string? RetryPolicyJson = null,
    string? CircuitPolicyJson = null
) : IRequest<bool>;

public class UpdateWebServiceMethodCommandHandler : IRequestHandler<UpdateWebServiceMethodCommand, bool>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<UpdateWebServiceMethodCommandHandler> _logger;

    public UpdateWebServiceMethodCommandHandler(DbContext dbContext, ILogger<UpdateWebServiceMethodCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateWebServiceMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await _dbContext.Set<WebServiceMethod>()
            .Include(m => m.WebService)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        
        if (method == null)
        {
            throw new ArgumentException($"WebServiceMethod with ID {request.Id} not found");
        }

        // Check for duplicate path within the same web service (excluding current method)
        var existingMethod = await _dbContext.Set<WebServiceMethod>()
            .AnyAsync(m => m.WebServiceId == method.WebServiceId && 
                          m.Path == request.Path && 
                          m.HttpMethod == request.HttpMethod.ToUpperInvariant() && 
                          m.Id != request.Id, cancellationToken);
        
        if (existingMethod)
        {
            throw new InvalidOperationException($"Method {request.HttpMethod} {request.Path} already exists for this web service");
        }

        method.Update(
            request.Path,
            request.HttpMethod,
            request.TimeoutMs,
            request.RequestSchema,
            request.ResponseSchema,
            request.RetryPolicyJson,
            request.CircuitPolicyJson
        );
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated WebServiceMethod {HttpMethod} {Path} with ID {Id}", 
            request.HttpMethod, request.Path, method.Id);

        return true;
    }
}