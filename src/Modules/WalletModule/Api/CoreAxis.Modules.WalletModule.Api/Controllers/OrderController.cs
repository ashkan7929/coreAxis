using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using CoreAxis.SharedKernel.Contracts;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.EventBus;
using CoreAxis.SharedKernel.Observability;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.WalletModule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IEventBus eventBus, ILogger<OrderController> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var correlationId = HttpContext.GetCorrelationIdAsGuid();
        
        _logger.LogInformation("Placing order for user {UserId}, asset {AssetCode}, quantity {Quantity} - CorrelationId: {CorrelationId}", 
            request.UserId, request.AssetCode, request.Quantity, correlationId);

        try
        {
            var orderId = Guid.NewGuid();
            var tenantId = HttpContext.User.FindFirst("tenant_id")?.Value ?? "default";

            // Create and publish OrderPlaced event
            var orderPlacedEvent = new OrderPlaced(
                orderId: orderId,
                userId: request.UserId,
                assetCode: request.AssetCode,
                quantity: request.Quantity,
                tenantId: tenantId,
                metadata: new Dictionary<string, object>
                {
                    { "correlationId", correlationId },
                    { "timestamp", DateTime.UtcNow },
                    { "source", "WalletModule.OrderController" }
                },
                correlationId: correlationId
            );

            await _eventBus.PublishAsync(orderPlacedEvent);

            _logger.LogInformation("Order placed successfully - OrderId: {OrderId}, CorrelationId: {CorrelationId}", 
                orderId, correlationId);

            return Ok(new PlaceOrderResponse
            {
                OrderId = orderId,
                Status = "Placed",
                Message = "Order placed successfully",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place order for user {UserId} - CorrelationId: {CorrelationId}", 
                request.UserId, correlationId);
            
            return StatusCode(500, new { Message = "Failed to place order", CorrelationId = correlationId });
        }
    }
}

public class PlaceOrderRequest
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(10, MinimumLength = 2)]
    public string AssetCode { get; set; } = string.Empty;
    
    [Required]
    [Range(0.000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public decimal Quantity { get; set; }
}

public class PlaceOrderResponse
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}