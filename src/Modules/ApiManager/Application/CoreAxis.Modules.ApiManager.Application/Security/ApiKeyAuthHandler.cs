using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.SharedKernel.Ports;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Security;

public class ApiKeyAuthHandler : IAuthSchemeHandler
{
    private readonly ILogger<ApiKeyAuthHandler> _logger;
    private readonly ISecretResolver _secretResolver;

    public ApiKeyAuthHandler(ILogger<ApiKeyAuthHandler> logger, ISecretResolver secretResolver)
    {
        _logger = logger;
        _secretResolver = secretResolver;
    }

    public SecurityType SupportedType => SecurityType.ApiKey;

    public async Task ApplyAsync(HttpRequestMessage request, SecurityProfile profile, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profile.ConfigJson)) return;

            var cfg = JsonSerializer.Deserialize<Dictionary<string, object>>(profile.ConfigJson);
            if (cfg == null) return;

            if (cfg.TryGetValue("headerName", out var headerNameObj) && cfg.TryGetValue("apiKey", out var apiKeyObj))
            {
                var headerName = headerNameObj?.ToString();
                var apiKey = apiKeyObj?.ToString();
                
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    apiKey = await _secretResolver.ResolveAsync(apiKey, cancellationToken) ?? apiKey;
                }

                if (!string.IsNullOrWhiteSpace(headerName) && !string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.TryAddWithoutValidation(headerName!, apiKey!);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApiKey handler failed for SecurityProfile {ProfileId}", profile.Id);
        }
    }
}