using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record InvokeApiMethodCommand(
    Guid MethodId,
    Dictionary<string, object> Parameters
) : IRequest<ApiProxyResult>;

public class InvokeApiMethodCommandHandler : IRequestHandler<InvokeApiMethodCommand, ApiProxyResult>
{
    private readonly DbContext _dbContext;
    private readonly IApiProxy _apiProxy;
    private readonly ILogger<InvokeApiMethodCommandHandler> _logger;

    public InvokeApiMethodCommandHandler(
        DbContext dbContext,
        IApiProxy apiProxy,
        ILogger<InvokeApiMethodCommandHandler> logger)
    {
        _dbContext = dbContext;
        _apiProxy = apiProxy;
        _logger = logger;
    }

    public async Task<ApiProxyResult> Handle(InvokeApiMethodCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invoking API method {MethodId} with {ParameterCount} parameters", 
            request.MethodId, request.Parameters.Count);

        // Get method details with web service and security profile
        var method = await _dbContext.Set<WebServiceMethod>()
            .Include(m => m.WebService)
                .ThenInclude(ws => ws.SecurityProfile)
            .Include(m => m.Parameters)
            .FirstOrDefaultAsync(m => m.Id == request.MethodId && m.IsActive, cancellationToken);

        if (method == null)
        {
            _logger.LogWarning("Method {MethodId} not found or inactive", request.MethodId);
            throw new ArgumentException($"Method with ID {request.MethodId} not found or inactive");
        }

        if (!method.WebService.IsActive)
        {
            _logger.LogWarning("Web service {WebServiceId} is inactive", method.WebService.Id);
            throw new InvalidOperationException($"Web service {method.WebService.Name} is inactive");
        }

        try
        {
            // Invoke the API method through the proxy service
            var result = await _apiProxy.InvokeAsync(
                method.WebService,
                method,
                request.Parameters,
                cancellationToken);

            _logger.LogInformation("API method {MethodId} invoked successfully. Success: {IsSuccess}, StatusCode: {StatusCode}, Latency: {LatencyMs}ms",
                request.MethodId, result.IsSuccess, result.StatusCode, result.LatencyMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking API method {MethodId}", request.MethodId);
            
            // Return error result
            return new ApiProxyResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                LatencyMs = 0
            };
        }
    }
}