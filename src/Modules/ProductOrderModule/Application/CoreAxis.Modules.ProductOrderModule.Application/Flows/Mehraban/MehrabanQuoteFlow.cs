using CoreAxis.Modules.ProductOrderModule.Application.DTOs.Quotes;
using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Connectors;
using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Flow;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Application.Flows.Mehraban;

public class MehrabanQuoteFlow : IQuoteFlow
{
    private readonly IRiskConnector _riskConnector;
    private readonly IFanavaranConnector _fanavaranConnector;
    private readonly ILogger<MehrabanQuoteFlow> _logger;

    public MehrabanQuoteFlow(
        IRiskConnector riskConnector, 
        IFanavaranConnector fanavaranConnector,
        ILogger<MehrabanQuoteFlow> logger)
    {
        _riskConnector = riskConnector;
        _fanavaranConnector = fanavaranConnector;
        _logger = logger;
    }

    public async Task<QuoteResponseDto> CalculateQuoteAsync(string applicationData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Mehraban Quote Calculation Flow.");

        // 0. Register/Get Customer ID from Fanavaran (Pre-requisite for Quote)
        // Note: The prompt implies this step is needed before price inquiry (which will be implemented next)
        _logger.LogInformation("Step 1: Registering/Getting Customer ID from Fanavaran...");
        var customerId = await _fanavaranConnector.CreateCustomerAsync(applicationData, cancellationToken);
        _logger.LogInformation("Step 1 Completed. Customer ID: {CustomerId}", customerId);
        
        // 1. Get Base Premium from Fanavaran
        _logger.LogInformation("Step 2: Getting Base Premium from Fanavaran for Customer ID: {CustomerId}...", customerId);
        decimal basePremium = await _fanavaranConnector.GetUniversalLifePriceAsync(customerId, applicationData, cancellationToken);
        _logger.LogInformation("Step 2 Completed. Base Premium: {BasePremium}", basePremium);

        // 2. Risk Evaluation (Mock Surcharge for now as per instructions)
        // "sum it with a surcharge which is fixed for now"
        _logger.LogInformation("Step 3: Calculating Risk Surcharge...");
        decimal riskSurcharge = 500000; // Fixed 50,000 Tomans (500,000 Rials)
        _logger.LogInformation("Step 3 Completed. Fixed Risk Surcharge: {RiskSurcharge}", riskSurcharge);
        
        // 3. Final Premium
        decimal finalPremium = basePremium + riskSurcharge;
        _logger.LogInformation("Step 4: Final Premium Calculated: {FinalPremium} (Base: {BasePremium} + Surcharge: {RiskSurcharge})", finalPremium, basePremium, riskSurcharge);

        // 4. Construct Response Blocks
        var response = new QuoteResponseDto
        {
            FinalPremium = finalPremium,
            RawVars = new { BasePremium = basePremium, RiskSurcharge = riskSurcharge }
        };

        // Price Breakdown
        var priceBlock = new PriceBreakdownBlock
        {
            Title = "جزئیات حق بیمه",
            Total = finalPremium,
            Items = new List<PriceItem>
            {
                new PriceItem { Label = "حق بیمه پایه", Amount = basePremium },
                new PriceItem { Label = "اضافه نرخ ریسک (ثابت)", Amount = riskSurcharge }
            }
        };
        response.Blocks.Add(priceBlock);

        // Commitments Table (Mock)
        var tableBlock = new TableBlock
        {
            Title = "تعهدات بیمه‌نامه",
            Headers = new List<string> { "پوشش", "سرمایه" },
            Rows = new List<List<string>>
            {
                new List<string> { "فوت به هر علت", "1,000,000,000 ریال" },
                new List<string> { "فوت ناشی از حادثه", "2,000,000,000 ریال" }
            }
        };
        response.Blocks.Add(tableBlock);

        _logger.LogInformation("Mehraban Quote Calculation Flow Completed Successfully.");
        return response;
    }
}
