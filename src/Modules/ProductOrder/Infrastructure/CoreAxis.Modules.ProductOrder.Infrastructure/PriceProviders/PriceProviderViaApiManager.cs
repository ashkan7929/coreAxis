using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ProductOrder.Domain.Entities;
using CoreAxis.Modules.ProductOrder.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrder.Infrastructure.PriceProviders;

public class PriceProviderViaApiManager : IPriceProvider
{
    private readonly IApiProxy _apiProxy;
    private readonly ILogger<PriceProviderViaApiManager> _logger;
    private readonly string _priceServiceName;
    private readonly string _getPriceMethodName;

    public PriceProviderViaApiManager(
        IApiProxy apiProxy,
        ILogger<PriceProviderViaApiManager> logger)
    {
        _apiProxy = apiProxy;
        _logger = logger;
        _priceServiceName = "PriceService";
        _getPriceMethodName = "GetPrice";
    }

    public async Task<decimal> GetPriceAsync(Product product, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting price for product {ProductId} via ApiManager", product.Id);

            // Prepare parameters for the price service call
            var parameters = new Dictionary<string, object>
            {
                { "productId", product.Id },
                { "productName", product.Name },
                { "category", product.Category }
            };

            // Call the price service through ApiManager
            var response = await _apiProxy.InvokeAsync(
                _priceServiceName,
                _getPriceMethodName,
                parameters,
                cancellationToken);

            if (response.IsSuccess && response.Data != null)
            {
                // Try to parse the price from the response
                if (TryParsePrice(response.Data, out decimal price))
                {
                    _logger.LogInformation("Successfully retrieved price {Price} for product {ProductId}", 
                        price, product.Id);
                    return price;
                }
                else
                {
                    _logger.LogWarning("Failed to parse price from response for product {ProductId}. Response: {Response}", 
                        product.Id, response.Data);
                }
            }
            else
            {
                _logger.LogWarning("Price service call failed for product {ProductId}. Error: {Error}", 
                    product.Id, response.ErrorMessage);
            }

            // Fallback to default pricing logic
            return GetFallbackPrice(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting price for product {ProductId}", product.Id);
            
            // Fallback to default pricing logic
            return GetFallbackPrice(product);
        }
    }

    private static bool TryParsePrice(object responseData, out decimal price)
    {
        price = 0;

        try
        {
            // Handle different response formats
            switch (responseData)
            {
                case decimal decimalPrice:
                    price = decimalPrice;
                    return true;
                
                case double doublePrice:
                    price = (decimal)doublePrice;
                    return true;
                
                case float floatPrice:
                    price = (decimal)floatPrice;
                    return true;
                
                case int intPrice:
                    price = intPrice;
                    return true;
                
                case string stringPrice when decimal.TryParse(stringPrice, out decimal parsedPrice):
                    price = parsedPrice;
                    return true;
                
                case JsonElement jsonElement:
                    return TryParseJsonPrice(jsonElement, out price);
                
                default:
                    // Try to parse as JSON string
                    if (responseData.ToString() is string jsonString)
                    {
                        try
                        {
                            var jsonDoc = JsonDocument.Parse(jsonString);
                            return TryParseJsonPrice(jsonDoc.RootElement, out price);
                        }
                        catch
                        {
                            // Ignore JSON parsing errors
                        }
                    }
                    break;
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return false;
    }

    private static bool TryParseJsonPrice(JsonElement jsonElement, out decimal price)
    {
        price = 0;

        try
        {
            // Try different property names for price
            string[] priceProperties = { "price", "Price", "amount", "Amount", "value", "Value" };

            foreach (var propertyName in priceProperties)
            {
                if (jsonElement.TryGetProperty(propertyName, out var priceProperty))
                {
                    if (priceProperty.ValueKind == JsonValueKind.Number)
                    {
                        price = priceProperty.GetDecimal();
                        return true;
                    }
                    else if (priceProperty.ValueKind == JsonValueKind.String)
                    {
                        if (decimal.TryParse(priceProperty.GetString(), out price))
                        {
                            return true;
                        }
                    }
                }
            }

            // If it's a simple number value
            if (jsonElement.ValueKind == JsonValueKind.Number)
            {
                price = jsonElement.GetDecimal();
                return true;
            }
        }
        catch
        {
            // Ignore JSON parsing errors
        }

        return false;
    }

    private decimal GetFallbackPrice(Product product)
    {
        _logger.LogInformation("Using fallback pricing for product {ProductId}", product.Id);
        
        // Simple fallback pricing logic based on product category
        return product.Category.ToLowerInvariant() switch
        {
            "electronics" => 299.99m,
            "clothing" => 49.99m,
            "books" => 19.99m,
            "food" => 9.99m,
            "toys" => 24.99m,
            _ => 99.99m // Default price
        };
    }
}