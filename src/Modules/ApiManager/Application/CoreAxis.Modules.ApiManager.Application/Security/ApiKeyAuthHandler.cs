using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Security;

public class ApiKeyAuthHandler : IAuthSchemeHandler
{
    private readonly ILogger<ApiKeyAuthHandler> _logger;

    public ApiKeyAuthHandler(ILogger<ApiKeyAuthHandler> logger)
    {
        _logger = logger;
    }

    public SecurityType SupportedType => SecurityType.ApiKey;

    public Task ApplyAsync(HttpRequestMessage request, SecurityProfile profile, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profile.ConfigJson)) return Task.CompletedTask;

            var cfg = JsonSerializer.Deserialize<Dictionary<string, object>>(profile.ConfigJson);
            if (cfg == null) return Task.CompletedTask;

            if (cfg.TryGetValue("headerName", out var headerNameObj) && cfg.TryGetValue("apiKey", out var apiKeyObj))
            {
                var headerName = headerNameObj?.ToString();
                var apiKey = apiKeyObj?.ToString();
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

        return Task.CompletedTask;
    }
}