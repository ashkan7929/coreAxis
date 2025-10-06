using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.SharedKernel.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Adapters;

public class PriceProviderViaApiManager : IPriceProvider
{
    private readonly IApiProxy _apiProxy;
    private readonly ILogger<PriceProviderViaApiManager> _logger;
    private readonly Guid _getPriceMethodId;

    public PriceProviderViaApiManager(
        IApiProxy apiProxy,
        ILogger<PriceProviderViaApiManager> logger,
        IConfiguration configuration)
    {
        _apiProxy = apiProxy;
        _logger = logger;

        var methodId = configuration.GetValue<string>("ApiManager:PriceService:GetPriceMethodId");
        if (string.IsNullOrWhiteSpace(methodId) || !Guid.TryParse(methodId, out var parsed))
        {
            _logger.LogWarning("ApiManager GetPrice method id is not configured correctly at 'ApiManager:PriceService:GetPriceMethodId'. Using Guid.Empty will likely fail.");
            _getPriceMethodId = Guid.Empty;
        }
        else
        {
            _getPriceMethodId = parsed;
        }
    }

    public async Task<PriceQuote> GetQuoteAsync(
        string assetCode,
        decimal quantity,
        PriceContext context,
        CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object>
        {
            ["assetCode"] = assetCode,
            ["quantity"] = quantity,
            ["tenantId"] = context.TenantId,
            ["userId"] = context.UserId,
            ["correlationId"] = context.CorrelationId,
            ["metadata"] = context.Metadata
        };

        var result = await _apiProxy.InvokeAsync(_getPriceMethodId, parameters, cancellationToken);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.ResponseBody))
        {
            _logger.LogWarning("GetPrice failed. StatusCode={StatusCode}, Error={Error}", result.StatusCode, result.ErrorMessage);
            throw new InvalidOperationException($"PriceProvider invocation failed: {result.ErrorMessage ?? "unknown error"}");
        }

        try
        {
            using var doc = JsonDocument.Parse(result.ResponseBody!);
            var root = doc.RootElement;

            var price = root.TryGetProperty("price", out var priceProp) ? priceProp.GetDecimal() : 0m;
            var providerId = root.TryGetProperty("providerId", out var providerProp) ? providerProp.GetString() ?? "ApiManager" : "ApiManager";
            var expiresInSeconds = root.TryGetProperty("expiresInSeconds", out var expProp) ? expProp.GetInt32() : 30;
            var timestamp = root.TryGetProperty("timestamp", out var tsProp) && tsProp.ValueKind == JsonValueKind.String
                ? DateTime.TryParse(tsProp.GetString(), out var dt) ? dt : DateTime.UtcNow
                : DateTime.UtcNow;

            return new PriceQuote(price, timestamp, providerId, expiresInSeconds, assetCode, quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse price quote response.");
            throw;
        }
    }
}