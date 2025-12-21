using MediatR;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record InvokeApiMethodCommand(
    Guid WebServiceMethodId,
    Dictionary<string, object> Parameters
) : IRequest<ApiProxyResult>;

public class InvokeApiMethodCommandHandler : IRequestHandler<InvokeApiMethodCommand, ApiProxyResult>
{
    private readonly IApiProxy _apiProxy;
    private readonly ILogger<InvokeApiMethodCommandHandler> _logger;

    public InvokeApiMethodCommandHandler(IApiProxy apiProxy, ILogger<InvokeApiMethodCommandHandler> logger)
    {
        _apiProxy = apiProxy;
        _logger = logger;
    }

    public async Task<ApiProxyResult> Handle(InvokeApiMethodCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invoking API method {MethodId} with {ParameterCount} parameters", 
            request.WebServiceMethodId, request.Parameters.Count);

        var result = await _apiProxy.InvokeAsync(
            request.WebServiceMethodId, 
            request.Parameters, 
            null,
            null,
            cancellationToken);

        _logger.LogInformation("API method {MethodId} invocation completed with success: {Success}, latency: {LatencyMs}ms", 
            request.WebServiceMethodId, result.IsSuccess, result.LatencyMs);

        return result;
    }
}