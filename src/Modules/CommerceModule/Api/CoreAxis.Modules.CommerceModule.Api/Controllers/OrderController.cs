using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.AuthModule.API.Authz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CoreAxis.Modules.CommerceModule.Api.Controllers;

/// <summary>
/// Controller for managing order operations
/// </summary>
[ApiController]
[Route("api/v1/commerce/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IPricingService _pricingService;
    private readonly ILogger<OrderController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderController"/> class
    /// </summary>
    /// <param name="orderService">The order service</param>
    /// <param name="pricingService">The pricing service</param>
    /// <param name="logger">The logger</param>
    public OrderController(
        IOrderService orderService,
        IPricingService pricingService,
        ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _pricingService = pricingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets orders with optional filtering
    /// </summary>
    /// <param name="customerId">Optional customer ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="fromDate">Optional from date filter</param>
    /// <param name="toDate">Optional to date filter</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>List of orders</returns>
    [HttpGet]
    [HasPermission("orders", "read")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
        [FromQuery] Guid? customerId = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            _logger.LogInformation("Getting orders with filters: CustomerId={CustomerId}, Status={Status}, FromDate={FromDate}, ToDate={ToDate}, Page={Page}, PageSize={PageSize}",
                customerId, status, fromDate, toDate, page, pageSize);

            var orders = await _orderService.GetOrdersAsync(
                customerId, status, fromDate, toDate, page, pageSize);

            var dtos = orders.Select(MapToDto).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return StatusCode(500, "An error occurred while retrieving orders");
        }
    }

    /// <summary>
    /// Gets a specific order by ID
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>The order</returns>
    [HttpGet("{id:guid}")]
    [HasPermission("orders", "read")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting order with ID: {Id}", id);

            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            return Ok(MapToDto(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the order");
        }
    }

    /// <summary>
    /// Creates a new order
    /// </summary>
    /// <param name="createDto">The order creation data</param>
    /// <returns>The created order</returns>
    [HttpPost]
    [HasPermission("orders", "create")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} creating new order for customer: {CustomerId}", userId, createDto.CustomerId);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = createDto.CustomerId,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
                ShippingAddress = createDto.ShippingAddress,
                BillingAddress = createDto.BillingAddress ?? createDto.ShippingAddress,
                Items = createDto.Items.Select(item => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.Quantity * item.UnitPrice
                }).ToList()
            };

            // Calculate pricing using snapshot-based pricing service
            var snapshot = MapToSnapshot(order);
            var pricingResult = await _pricingService.ApplyDiscountsAsync(snapshot, null, null, HttpContext.RequestAborted);
            order.SubtotalAmount = pricingResult.SubtotalAmount;
            order.DiscountAmount = pricingResult.DiscountAmount;
            order.TaxAmount = pricingResult.TaxAmount;
            order.TotalAmount = pricingResult.TotalAmount;

            var createdOrder = await _orderService.CreateOrderAsync(order);

            _logger.LogInformation("Successfully created order with ID: {Id}", createdOrder.Id);

            return CreatedAtAction(
                nameof(GetOrder),
                new { id = createdOrder.Id },
                MapToDto(createdOrder));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating order");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating order");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, "An error occurred while creating the order");
        }
    }

    /// <summary>
    /// Updates an existing order
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <param name="updateDto">The order update data</param>
    /// <returns>The updated order</returns>
    [HttpPut("{id:guid}")]
    [HasPermission("orders", "update")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> UpdateOrder(Guid id, [FromBody] UpdateOrderDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} updating order with ID: {Id}", userId, id);

            var existingOrder = await _orderService.GetOrderByIdAsync(id);
            if (existingOrder == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            // Only allow updates for certain statuses
            if (existingOrder.Status != OrderStatus.Pending && existingOrder.Status != OrderStatus.Confirmed)
            {
                return BadRequest($"Cannot update order in {existingOrder.Status} status");
            }

            // Update allowed fields
            if (!string.IsNullOrEmpty(updateDto.ShippingAddress))
                existingOrder.ShippingAddress = updateDto.ShippingAddress;
            if (!string.IsNullOrEmpty(updateDto.BillingAddress))
                existingOrder.BillingAddress = updateDto.BillingAddress;
            if (updateDto.Status.HasValue)
                existingOrder.Status = updateDto.Status.Value;

            existingOrder.LastModified = DateTime.UtcNow;

            var updatedOrder = await _orderService.UpdateOrderAsync(existingOrder);

            _logger.LogInformation("Successfully updated order with ID: {Id}", id);

            return Ok(MapToDto(updatedOrder));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating order with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order with ID: {Id}", id);
            return StatusCode(500, "An error occurred while updating the order");
        }
    }

    /// <summary>
    /// Cancels an order
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <param name="reason">The cancellation reason</param>
    /// <returns>The cancelled order</returns>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission("orders", "cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> CancelOrder(Guid id, [FromBody] CancelOrderDto cancelDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} cancelling order with ID: {Id}, Reason: {Reason}", userId, id, cancelDto.Reason);

            var cancelledOrder = await _orderService.CancelOrderAsync(id, cancelDto.Reason);
            if (cancelledOrder == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            _logger.LogInformation("Successfully cancelled order with ID: {Id}", id);

            return Ok(MapToDto(cancelledOrder));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when cancelling order with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order with ID: {Id}", id);
            return StatusCode(500, "An error occurred while cancelling the order");
        }
    }

    /// <summary>
    /// Confirms an order (moves from Pending to Confirmed)
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>The confirmed order</returns>
    [HttpPost("{id:guid}/confirm")]
    [HasPermission("orders", "confirm")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> ConfirmOrder(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} confirming order with ID: {Id}", userId, id);

            var confirmedOrder = await _orderService.ConfirmOrderAsync(id);
            if (confirmedOrder == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            _logger.LogInformation("Successfully confirmed order with ID: {Id}", id);

            return Ok(MapToDto(confirmedOrder));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when confirming order with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order with ID: {Id}", id);
            return StatusCode(500, "An error occurred while confirming the order");
        }
    }

    /// <summary>
    /// Fulfills an order (moves to Fulfilled status)
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <param name="fulfillDto">The fulfillment data</param>
    /// <returns>The fulfilled order</returns>
    [HttpPost("{id:guid}/fulfill")]
    [HasPermission("orders", "fulfill")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> FulfillOrder(Guid id, [FromBody] FulfillOrderDto fulfillDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} fulfilling order with ID: {Id}, TrackingNumber: {TrackingNumber}", 
                userId, id, fulfillDto.TrackingNumber);

            var fulfilledOrder = await _orderService.FulfillOrderAsync(id, fulfillDto.TrackingNumber, fulfillDto.ShippingCarrier);
            if (fulfilledOrder == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            _logger.LogInformation("Successfully fulfilled order with ID: {Id}", id);

            return Ok(MapToDto(fulfilledOrder));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when fulfilling order with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fulfilling order with ID: {Id}", id);
            return StatusCode(500, "An error occurred while fulfilling the order");
        }
    }

    #region Private Methods

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status,
            OrderDate = order.OrderDate,
            ShippingAddress = order.ShippingAddress,
            BillingAddress = order.BillingAddress,
            SubtotalAmount = order.SubtotalAmount,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            TrackingNumber = order.TrackingNumber,
            ShippingCarrier = order.ShippingCarrier,
            LastModified = order.LastModified,
            Items = order.Items?.Select(item => new OrderItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList() ?? new List<OrderItemDto>()
        };
    }

    private static OrderSnapshot MapToSnapshot(Order order)
    {
        return new OrderSnapshot
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            SubtotalAmount = order.SubtotalAmount,
            Currency = order.Currency,
            Metadata = order.Metadata,
            Items = order.Items?.Select(item => new OrderItemSnapshot
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                CategoryIds = item.CategoryIds
            }).ToList() ?? new List<OrderItemSnapshot>()
        };
    }

    #endregion
}