using CoreAxis.SharedKernel.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CoreAxis.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/public/quotes")]
    public class PublicQuotesController : ControllerBase
    {
        private readonly IPriceProvider _priceProvider;
        private readonly ILogger<PublicQuotesController> _logger;

        public PublicQuotesController(IPriceProvider priceProvider, ILogger<PublicQuotesController> logger)
        {
            _priceProvider = priceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Get a public price quote for Gold (XAU).
        /// </summary>
        /// <param name="quantity">Quantity to quote (default 1). Interpreted per provider rules.</param>
        /// <returns>Price quote details including expiration and provider metadata.</returns>
        [HttpGet("gold")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetGoldQuote([FromQuery] decimal quantity = 1)
        {
            if (quantity <= 0)
            {
                return BadRequest("Quantity must be greater than 0.");
            }

            var correlationIdHeader = Request.Headers["X-Correlation-ID"].FirstOrDefault();
            var correlationId = Guid.TryParse(correlationIdHeader, out var cid) ? cid : Guid.NewGuid();

            var context = new PriceContext(
                tenantId: "public",
                userId: Guid.Empty,
                correlationId: correlationId,
                metadata: new Dictionary<string, object>
                {
                    ["ip"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    ["ua"] = Request.Headers["User-Agent"].ToString()
                }
            );

            try
            {
                var quote = await _priceProvider.GetQuoteAsync("XAU", quantity, context, HttpContext.RequestAborted);

                var response = new
                {
                    quoteId = Guid.NewGuid().ToString("N"),
                    assetCode = quote.AssetCode,
                    quantity = quote.Quantity,
                    price = quote.Price,
                    timestamp = quote.Timestamp,
                    providerId = quote.ProviderId,
                    expiresInSeconds = quote.ExpiresInSeconds,
                    expiresAt = quote.Timestamp.AddSeconds(quote.ExpiresInSeconds)
                };

                _logger.LogInformation("Public gold quote: {Price} for {Quantity} {Asset} (provider {Provider})",
                    quote.Price, quote.Quantity, quote.AssetCode, quote.ProviderId);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for gold quote");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating gold quote");
                return StatusCode(500, "An error occurred while generating the quote.");
            }
        }
    }
}