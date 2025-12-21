using MediatR;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record InvokeApiEndpointCommand(
    string ServiceName,
    string HttpMethod,
    string Path,
    Dictionary<string, object> Parameters
) : IRequest<ApiProxyResult>;

public class InvokeApiEndpointCommandHandler : IRequestHandler<InvokeApiEndpointCommand, ApiProxyResult>
{
    private readonly DbContext _dbContext;
    private readonly IApiProxy _apiProxy;
    private readonly ILogger<InvokeApiEndpointCommandHandler> _logger;

    public InvokeApiEndpointCommandHandler(
        DbContext dbContext,
        IApiProxy apiProxy,
        ILogger<InvokeApiEndpointCommandHandler> logger)
    {
        _dbContext = dbContext;
        _apiProxy = apiProxy;
        _logger = logger;
    }

    public async Task<ApiProxyResult> Handle(InvokeApiEndpointCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invoking API endpoint {Service}/{Method} {Path}", request.ServiceName, request.HttpMethod, request.Path);

        // Find method by service name + http method + path
        var method = await _dbContext.Set<WebServiceMethod>()
            .Include(m => m.WebService)
                .ThenInclude(ws => ws.SecurityProfile)
            .Include(m => m.Parameters)
            .FirstOrDefaultAsync(m => m.IsActive
                                     && m.WebService.IsActive
                                     && m.WebService.Name == request.ServiceName
                                     && m.HttpMethod == request.HttpMethod
                                     && m.Path == request.Path,
                cancellationToken);

        if (method == null)
        {
            var msg = $"Endpoint not found: service={request.ServiceName}, method={request.HttpMethod}, path={request.Path}";
            _logger.LogWarning(msg);
            return ApiProxyResult.Failure(msg, 0, 404);
        }

        try
        {
            var result = await _apiProxy.InvokeAsync(method.Id, request.Parameters, null, null, cancellationToken);
            _logger.LogInformation("Endpoint invoked. Success={IsSuccess}, Status={Status}", result.IsSuccess, result.StatusCode);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking endpoint {Service}/{Method} {Path}", request.ServiceName, request.HttpMethod, request.Path);
            return ApiProxyResult.Failure(ex.Message, 0);
        }
    }
}