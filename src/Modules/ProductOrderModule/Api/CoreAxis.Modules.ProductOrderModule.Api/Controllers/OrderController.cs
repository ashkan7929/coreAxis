using CoreAxis.Modules.ProductOrderModule.Application.Commands;
using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Application.Queries;
using CoreAxis.Modules.AuthModule.API.Authz;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAxis.Modules.ProductOrderModule.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Place a new order
    /// </summary>
    [HttpPost]
    [HasPermission("ORDER", "PLACE")]
    public async Task<ActionResult<OrderDto>> PlaceOrder([FromBody] PlaceOrderDto request)
    {
        var userId = GetUserId();
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault();
        
        var command = new PlaceOrderCommand
        {
            UserId = userId,
            AssetCode = request.AssetCode,
            TotalAmount = request.TotalAmount,
            OrderLines = request.OrderLines,
            IdempotencyKey = idempotencyKey
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("ORDER", "READ")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        var userId = GetUserId();
        var query = new GetOrderQuery { OrderId = id, UserId = userId };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Get orders for current user
    /// </summary>
    [HttpGet]
    [HasPermission("ORDER", "READ")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetUserOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var query = new GetUserOrdersQuery
        {
            UserId = userId,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    [HttpPost("{id}/cancel")]
    [HasPermission("ORDER", "CANCEL")]
    public async Task<ActionResult> CancelOrder(Guid id)
    {
        var userId = GetUserId();
        var command = new CancelOrderCommand
        {
            OrderId = id,
            UserId = userId
        };

        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Get order lines for an order
    /// </summary>
    [HttpGet("{id}/lines")]
    [HasPermission("ORDER", "READ")]
    public async Task<ActionResult<IEnumerable<OrderLineDto>>> GetOrderLines(Guid id)
    {
        var userId = GetUserId();
        var query = new GetOrderLinesQuery { OrderId = id, UserId = userId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    private string GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim != null)
        {
            return userIdClaim.Value;
        }
        throw new UnauthorizedAccessException("User ID not found in claims");
    }
}