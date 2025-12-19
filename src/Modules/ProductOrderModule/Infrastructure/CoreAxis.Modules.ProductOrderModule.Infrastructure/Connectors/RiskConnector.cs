using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Connectors;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Connectors;

public class RiskConnector : IRiskConnector
{
    private readonly ILogger<RiskConnector> _logger;
    private readonly HttpClient _httpClient;

    public RiskConnector(ILogger<RiskConnector> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<decimal> CalculateRiskAsync(string healthData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calculating risk...");
        await Task.Delay(50, cancellationToken);
        // Mock logic: 10% risk loading
        return 1.10m;
    }
}
