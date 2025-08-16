using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Examples;

/// <summary>
/// Example implementation of a price provider using ApiManager
/// This demonstrates how to integrate external APIs using the ApiManager module
/// </summary>
public class PriceProviderExample
{
    private readonly IApiProxy _apiProxy;
    private readonly ILogger<PriceProviderExample> _logger;
    
    public PriceProviderExample(IApiProxy apiProxy, ILogger<PriceProviderExample> logger)
    {
        _apiProxy = apiProxy;
        _logger = logger;
    }
    
    /// <summary>
    /// Get current price for a cryptocurrency pair
    /// </summary>
    /// <param name="baseCurrency">Base currency (e.g., BTC)</param>
    /// <param name="quoteCurrency">Quote currency (e.g., USD)</param>
    /// <returns>Current price or null if failed</returns>
    public async Task<decimal?> GetPriceAsync(string baseCurrency, string quoteCurrency)
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "symbol", $"{baseCurrency}{quoteCurrency}" }
            };
            
            var response = await _apiProxy.InvokeAsync<PriceResponse>(
                "Binance API", // Web service name
                "/api/v3/ticker/price", // Method path
                parameters
            );
            
            if (response.IsSuccess && response.Value != null)
            {
                _logger.LogInformation(
                    "Successfully retrieved price for {Symbol}: {Price}",
                    $"{baseCurrency}{quoteCurrency}",
                    response.Value.Price
                );
                
                return response.Value.Price;
            }
            
            _logger.LogWarning(
                "Failed to retrieve price for {Symbol}: {Error}",
                $"{baseCurrency}{quoteCurrency}",
                response.Error
            );
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Exception occurred while retrieving price for {Symbol}",
                $"{baseCurrency}{quoteCurrency}"
            );
            
            return null;
        }
    }
    
    /// <summary>
    /// Get historical prices for a cryptocurrency pair
    /// </summary>
    /// <param name="baseCurrency">Base currency</param>
    /// <param name="quoteCurrency">Quote currency</param>
    /// <param name="interval">Time interval (1m, 5m, 1h, 1d, etc.)</param>
    /// <param name="limit">Number of data points to retrieve</param>
    /// <returns>List of historical prices</returns>
    public async Task<List<HistoricalPrice>?> GetHistoricalPricesAsync(
        string baseCurrency, 
        string quoteCurrency, 
        string interval = "1d", 
        int limit = 30)
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "symbol", $"{baseCurrency}{quoteCurrency}" },
                { "interval", interval },
                { "limit", limit }
            };
            
            var response = await _apiProxy.InvokeAsync<List<object[]>>(
                "Binance API",
                "/api/v3/klines",
                parameters
            );
            
            if (response.IsSuccess && response.Value != null)
            {
                var historicalPrices = response.Value.Select(kline => new HistoricalPrice
                {
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(kline[0])),
                    Open = Convert.ToDecimal(kline[1]),
                    High = Convert.ToDecimal(kline[2]),
                    Low = Convert.ToDecimal(kline[3]),
                    Close = Convert.ToDecimal(kline[4]),
                    Volume = Convert.ToDecimal(kline[5])
                }).ToList();
                
                _logger.LogInformation(
                    "Successfully retrieved {Count} historical prices for {Symbol}",
                    historicalPrices.Count,
                    $"{baseCurrency}{quoteCurrency}"
                );
                
                return historicalPrices;
            }
            
            _logger.LogWarning(
                "Failed to retrieve historical prices for {Symbol}: {Error}",
                $"{baseCurrency}{quoteCurrency}",
                response.Error
            );
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while retrieving historical prices for {Symbol}",
                $"{baseCurrency}{quoteCurrency}"
            );
            
            return null;
        }
    }
    
    /// <summary>
    /// Validate if a trading pair is supported
    /// </summary>
    /// <param name="baseCurrency">Base currency</param>
    /// <param name="quoteCurrency">Quote currency</param>
    /// <returns>True if supported, false otherwise</returns>
    public async Task<bool> IsPairSupportedAsync(string baseCurrency, string quoteCurrency)
    {
        try
        {
            var response = await _apiProxy.InvokeAsync<ExchangeInfo>(
                "Binance API",
                "/api/v3/exchangeInfo",
                new Dictionary<string, object>()
            );
            
            if (response.IsSuccess && response.Value?.Symbols != null)
            {
                var symbol = $"{baseCurrency}{quoteCurrency}";
                var isSupported = response.Value.Symbols.Any(s => 
                    s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase) && 
                    s.Status == "TRADING"
                );
                
                _logger.LogInformation(
                    "Pair {Symbol} support check: {IsSupported}",
                    symbol,
                    isSupported
                );
                
                return isSupported;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred while checking pair support for {Symbol}",
                $"{baseCurrency}{quoteCurrency}"
            );
            
            return false;
        }
    }
}

/// <summary>
/// Response model for current price API
/// </summary>
public class PriceResponse
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>
/// Model for historical price data
/// </summary>
public class HistoricalPrice
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}

/// <summary>
/// Exchange information response model
/// </summary>
public class ExchangeInfo
{
    public List<SymbolInfo> Symbols { get; set; } = new();
}

/// <summary>
/// Symbol information model
/// </summary>
public class SymbolInfo
{
    public string Symbol { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BaseAsset { get; set; } = string.Empty;
    public string QuoteAsset { get; set; } = string.Empty;
}