using CoreAxis.SharedKernel.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Adapters.Stubs;

public class InMemoryPriceProvider : IPriceProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<InMemoryPriceProvider> _logger;
    private readonly Dictionary<string, decimal> _basePrices;
    private readonly Random _random = new();

    public InMemoryPriceProvider(IConfiguration configuration, ILogger<InMemoryPriceProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Initialize base prices from configuration or defaults
        _basePrices = new Dictionary<string, decimal>
        {
            { "XAU", _configuration.GetValue<decimal>("PriceProvider:BasePrices:XAU", 2000.00m) }, // Gold per ounce
            { "IRR", _configuration.GetValue<decimal>("PriceProvider:BasePrices:IRR", 42000.00m) }, // Iranian Rial per USD
            { "BTC", _configuration.GetValue<decimal>("PriceProvider:BasePrices:BTC", 45000.00m) }, // Bitcoin per USD
            { "ETH", _configuration.GetValue<decimal>("PriceProvider:BasePrices:ETH", 3000.00m) }, // Ethereum per USD
            { "USD", _configuration.GetValue<decimal>("PriceProvider:BasePrices:USD", 1.00m) }, // USD base
            { "EUR", _configuration.GetValue<decimal>("PriceProvider:BasePrices:EUR", 1.08m) }, // Euro per USD
        };
    }

    public async Task<PriceQuote> GetQuoteAsync(string assetCode, decimal quantity, PriceContext context, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate network delay

        if (!_basePrices.TryGetValue(assetCode.ToUpper(), out var basePrice))
        {
            throw new ArgumentException($"Asset code '{assetCode}' is not supported", nameof(assetCode));
        }

        // Apply spread and volatility
        var spread = _configuration.GetValue<decimal>("PriceProvider:Spread", 0.02m); // 2% default spread
        var volatility = _configuration.GetValue<decimal>("PriceProvider:Volatility", 0.05m); // 5% volatility
        
        // Generate price with some randomness
        var volatilityFactor = 1 + ((_random.NextDouble() - 0.5) * 2 * (double)volatility);
        var spreadFactor = 1 + (double)spread;
        
        var finalPrice = basePrice * (decimal)volatilityFactor * (decimal)spreadFactor;
        
        var expiresIn = _configuration.GetValue<int>("PriceProvider:ExpiresInSeconds", 300); // 5 minutes default
        
        var quote = new PriceQuote(
            price: Math.Round(finalPrice, 6),
            timestamp: DateTime.UtcNow,
            providerId: "InMemoryProvider",
            expiresInSeconds: expiresIn,
            assetCode: assetCode.ToUpper(),
            quantity: quantity
        );

        _logger.LogInformation("Generated price quote for {AssetCode}: {Price} (Quantity: {Quantity})", 
            assetCode, quote.Price, quantity);

        return quote;
    }
}