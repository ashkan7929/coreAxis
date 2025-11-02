using CoreAxis.Modules.ProductOrderModule.Application.Commands;
using CoreAxis.Modules.ProductOrderModule.Application.DTOs;
using CoreAxis.Modules.ProductOrderModule.Application.Queries;
using CoreAxis.Modules.AuthModule.API.Authz;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;

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
    /// Place a new order.
    /// </summary>
    /// <remarks>
    /// Headers:
    /// - `Idempotency-Key` (optional GUID) to ensure safe retries.
    /// - `X-Correlation-ID` (optional GUID) for tracing.
    ///
    /// Request body:
    /// ```json
    /// {
    ///   "assetCode": "PRD-001",
    ///   "totalAmount": 149.99,
    ///   "orderLines": [
    ///     { "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "quantity": 2, "unitPrice": 74.995 }
    ///   ]
    /// }
    /// ```
    ///
    /// Responses:
    /// - 201 Created → returns `OrderDto` with `Location` header to `GET /api/order/{id}`.
    /// - 400 BadRequest → validation errors.
    /// - 401 Unauthorized → user not authenticated.
    /// - 500 InternalServerError.
    /// </remarks>
    [HttpPost]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    //[HasPermission("ORDER", "PLACE")]
    public async Task<ActionResult<OrderDto>> PlaceOrder([FromHeader] string tenantId, [FromBody] PlaceOrderDto request)
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
            IdempotencyKey = idempotencyKey,
            TenantId = tenantId
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get order by ID.
    /// </summary>
    /// <remarks>
    /// Returns `404` when the order is not found or does not belong to the current user.
    /// </remarks>
    [HttpGet("{id}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    //[HasPermission("ORDER", "READ")]
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
    /// Get orders for current user (paged).
    /// </summary>
    /// <remarks>
    /// Query:
    /// - `page` (default 1)
    /// - `pageSize` (default 20)
    ///
    /// Responses:
    /// - 200 OK → list of `OrderDto`.
    /// - 401 Unauthorized.
    /// - 500 InternalServerError.
    /// </remarks>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    //[HasPermission("ORDER", "READ")]
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
    /// Cancel an order.
    /// </summary>
    /// <remarks>
    /// Responses:
    /// - 204 NoContent → canceled.
    /// - 404 NotFound → order not found.
    /// - 401 Unauthorized.
    /// - 500 InternalServerError.
    /// </remarks>
    [HttpPost("{id}/cancel")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    //[HasPermission("ORDER", "CANCEL")]
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
    /// Get order lines for an order.
    /// </summary>
    /// <remarks>
    /// Responses:
    /// - 200 OK → list of `OrderLineDto`.
    /// - 404 NotFound → order not found.
    /// - 401 Unauthorized.
    /// - 500 InternalServerError.
    /// </remarks>
    [HttpGet("{id}/lines")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(IEnumerable<OrderLineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    //[HasPermission("ORDER", "READ")]
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