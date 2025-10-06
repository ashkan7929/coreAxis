using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Security;

public class AuthSchemeHandlerResolver : IAuthSchemeHandlerResolver
{
    private readonly IEnumerable<IAuthSchemeHandler> _handlers;
    private readonly ILogger<AuthSchemeHandlerResolver> _logger;

    public AuthSchemeHandlerResolver(IEnumerable<IAuthSchemeHandler> handlers, ILogger<AuthSchemeHandlerResolver> logger)
    {
        _handlers = handlers;
        _logger = logger;
    }

    public async Task ApplyAsync(HttpRequestMessage request, SecurityProfile profile, CancellationToken cancellationToken)
    {
        if (profile == null) return;

        var handler = _handlers.FirstOrDefault(h => h.SupportedType == profile.Type);
        if (handler == null)
        {
            _logger.LogDebug("No auth handler registered for type {Type}", profile.Type);
            return;
        }

        await handler.ApplyAsync(request, profile, cancellationToken);
    }
}