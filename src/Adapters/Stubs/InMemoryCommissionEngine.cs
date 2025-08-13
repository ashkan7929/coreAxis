using CoreAxis.SharedKernel.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Adapters.Stubs;

public class InMemoryCommissionEngine : ICommissionEngine
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<InMemoryCommissionEngine> _logger;

    public InMemoryCommissionEngine(IConfiguration configuration, ILogger<InMemoryCommissionEngine> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CommissionResult> CalculateAsync(PaymentContext paymentContext, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate processing

        // Get commission rates from configuration
        var baseRate = _configuration.GetValue<decimal>("Commission:BaseRate", 0.02m); // 2% default
        var minimumFee = _configuration.GetValue<decimal>("Commission:MinimumFee", 1.00m);
        var maximumFee = _configuration.GetValue<decimal>("Commission:MaximumFee", 100.00m);

        // Calculate commission based on amount
        var calculatedFee = paymentContext.Amount * baseRate;
        var finalFee = Math.Max(minimumFee, Math.Min(maximumFee, calculatedFee));

        var result = new CommissionResult(
            originalAmount: paymentContext.Amount,
            commissionAmount: Math.Round(finalFee, 2),
            netAmount: paymentContext.Amount - Math.Round(finalFee, 2),
            commissionRate: baseRate,
            calculationMethod: "Percentage",
            metadata: new Dictionary<string, object>
            {
                { "baseRate", baseRate },
                { "minimumFee", minimumFee },
                { "maximumFee", maximumFee },
                { "calculatedFee", calculatedFee }
            }
        );

        _logger.LogInformation("Calculated commission: {CommissionAmount} for amount {Amount} (Rate: {Rate}%)", 
            result.CommissionAmount, paymentContext.Amount, baseRate * 100);

        return result;
    }
}