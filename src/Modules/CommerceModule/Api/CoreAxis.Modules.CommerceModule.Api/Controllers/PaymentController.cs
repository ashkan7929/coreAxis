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
/// Controller for managing payment operations
/// </summary>
[ApiController]
[Route("api/v1/commerce/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;
    private readonly ILogger<PaymentController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentController"/> class
    /// </summary>
    /// <param name="paymentService">The payment service</param>
    /// <param name="orderService">The order service</param>
    /// <param name="logger">The logger</param>
    public PaymentController(
        IPaymentService paymentService,
        IOrderService orderService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Gets payments with optional filtering
    /// </summary>
    /// <param name="orderId">Optional order ID filter</param>
    /// <param name="customerId">Optional customer ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="fromDate">Optional from date filter</param>
    /// <param name="toDate">Optional to date filter</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>List of payments</returns>
    [HttpGet]
    [HasPermission("payments", "read")]
    [ProducesResponseType(typeof(IEnumerable<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments(
        [FromQuery] Guid? orderId = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            _logger.LogInformation("Getting payments with filters: OrderId={OrderId}, CustomerId={CustomerId}, Status={Status}, FromDate={FromDate}, ToDate={ToDate}, Page={Page}, PageSize={PageSize}",
                orderId, customerId, status, fromDate, toDate, page, pageSize);

            var payments = await _paymentService.GetPaymentsAsync(
                orderId, customerId, status, fromDate, toDate, page, pageSize);

            var dtos = payments.Select(MapToDto).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments");
            return StatusCode(500, "An error occurred while retrieving payments");
        }
    }

    /// <summary>
    /// Gets a specific payment by ID
    /// </summary>
    /// <param name="id">The payment ID</param>
    /// <returns>The payment</returns>
    [HttpGet("{id:guid}")]
    [HasPermission("payments", "read")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentDto>> GetPayment(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting payment with ID: {Id}", id);

            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                return NotFound($"Payment with ID {id} not found");
            }

            return Ok(MapToDto(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the payment");
        }
    }

    /// <summary>
    /// Process a payment for an order.
    /// </summary>
    /// <param name="processDto">Payment data including order, amount, method, idempotency.</param>
    /// <returns>Processed payment details.</returns>
    /// <remarks>
    /// Use the <c>Idempotency-Key</c> in the payload to avoid duplicate charges.
    /// 
    /// Sample request:
    /// 
    /// {
    ///   "orderId": "00000000-0000-0000-0000-000000000001",
    ///   "amount": 250.00,
    ///   "paymentMethod": "Card",
    ///   "idempotencyKey": "123e4567-e89b-12d3-a456-426614174000"
    /// }
    /// 
    /// Possible responses:
    /// - 200 OK: Payment processed successfully
    /// - 400 BadRequest: Validation error or invalid order state/amount
    /// - 404 NotFound: Order not found
    /// - 500 InternalServerError: Unexpected error
    /// </remarks>
    [HttpPost("process")]
    [HasPermission("payments", "process")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentDto>> ProcessPayment([FromBody] ProcessPaymentDto processDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} processing payment for order: {OrderId}, Amount: {Amount}", 
                userId, processDto.OrderId, processDto.Amount);

            // Verify order exists and is in correct state
            var order = await _orderService.GetOrderByIdAsync(processDto.OrderId);
            if (order == null)
            {
                return NotFound($"Order with ID {processDto.OrderId} not found");
            }

            if (order.Status != OrderStatus.Confirmed)
            {
                return BadRequest($"Cannot process payment for order in {order.Status} status");
            }

            // Verify payment amount matches order total
            if (processDto.Amount != order.TotalAmount)
            {
                return BadRequest($"Payment amount {processDto.Amount} does not match order total {order.TotalAmount}");
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = processDto.OrderId,
                CustomerId = order.CustomerId,
                Amount = processDto.Amount,
                PaymentMethod = processDto.PaymentMethod,
                Status = PaymentStatus.Pending,
                ProcessedAt = DateTime.UtcNow,
                IdempotencyKey = processDto.IdempotencyKey
            };

            var processedPayment = await _paymentService.ProcessPaymentAsync(payment);

            _logger.LogInformation("Successfully processed payment with ID: {Id}, Status: {Status}", 
                processedPayment.Id, processedPayment.Status);

            return Ok(MapToDto(processedPayment));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when processing payment");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when processing payment");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return StatusCode(500, "An error occurred while processing the payment");
        }
    }

    /// <summary>
    /// Process a refund for a completed payment.
    /// </summary>
    /// <param name="paymentId">Payment ID to refund.</param>
    /// <param name="refundDto">Refund data including amount, reason, idempotency.</param>
    /// <returns>Processed refund details.</returns>
    /// <remarks>
    /// Refund amount must be between 0 and original payment amount.
    /// Use the <c>Idempotency-Key</c> to avoid duplicate refunds.
    /// 
    /// Sample request:
    /// 
    /// {
    ///   "amount": 100.00,
    ///   "reason": "Customer returned item",
    ///   "idempotencyKey": "123e4567-e89b-12d3-a456-426614174001"
    /// }
    /// 
    /// Possible responses:
    /// - 200 OK: Refund processed successfully
    /// - 400 BadRequest: Validation error or invalid payment state/amount
    /// - 404 NotFound: Payment not found
    /// - 500 InternalServerError: Unexpected error
    /// </remarks>
    [HttpPost("{paymentId:guid}/refund")]
    [HasPermission("payments", "refund")]
    [ProducesResponseType(typeof(RefundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RefundDto>> ProcessRefund(Guid paymentId, [FromBody] ProcessRefundDto refundDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} processing refund for payment: {PaymentId}, Amount: {Amount}, Reason: {Reason}", 
                userId, paymentId, refundDto.Amount, refundDto.Reason);

            // Verify payment exists and is in correct state
            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                return NotFound($"Payment with ID {paymentId} not found");
            }

            if (payment.Status != PaymentStatus.Completed)
            {
                return BadRequest($"Cannot refund payment in {payment.Status} status");
            }

            // Verify refund amount is valid
            if (refundDto.Amount <= 0 || refundDto.Amount > payment.Amount)
            {
                return BadRequest($"Invalid refund amount. Must be between 0 and {payment.Amount}");
            }

            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                PaymentId = paymentId,
                Amount = refundDto.Amount,
                Reason = refundDto.Reason,
                Status = RefundStatus.Pending,
                ProcessedAt = DateTime.UtcNow,
                IdempotencyKey = refundDto.IdempotencyKey
            };

            var processedRefund = await _paymentService.ProcessRefundAsync(refund);

            _logger.LogInformation("Successfully processed refund with ID: {Id}, Status: {Status}", 
                processedRefund.Id, processedRefund.Status);

            return Ok(MapToRefundDto(processedRefund));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when processing refund for payment: {PaymentId}", paymentId);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when processing refund for payment: {PaymentId}", paymentId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment: {PaymentId}", paymentId);
            return StatusCode(500, "An error occurred while processing the refund");
        }
    }

    /// <summary>
    /// Gets refunds for a specific payment
    /// </summary>
    /// <param name="paymentId">The payment ID</param>
    /// <returns>List of refunds for the payment</returns>
    [HttpGet("{paymentId:guid}/refunds")]
    [HasPermission("payments", "read")]
    [ProducesResponseType(typeof(IEnumerable<RefundDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<RefundDto>>> GetPaymentRefunds(Guid paymentId)
    {
        try
        {
            _logger.LogInformation("Getting refunds for payment: {PaymentId}", paymentId);

            var refunds = await _paymentService.GetRefundsByPaymentIdAsync(paymentId);
            var dtos = refunds.Select(MapToRefundDto).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refunds for payment: {PaymentId}", paymentId);
            return StatusCode(500, "An error occurred while retrieving refunds");
        }
    }

    /// <summary>
    /// Verifies payment status with external provider
    /// </summary>
    /// <param name="id">The payment ID</param>
    /// <returns>The updated payment status</returns>
    [HttpPost("{id:guid}/verify")]
    [HasPermission("payments", "verify")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentDto>> VerifyPayment(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} verifying payment: {PaymentId}", userId, id);

            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                return NotFound($"Payment with ID {id} not found");
            }

            var verifiedPayment = await _paymentService.VerifyPaymentStatusAsync(id);

            _logger.LogInformation("Payment verification completed for ID: {Id}, Status: {Status}", 
                id, verifiedPayment.Status);

            return Ok(MapToDto(verifiedPayment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment: {PaymentId}", id);
            return StatusCode(500, "An error occurred while verifying the payment");
        }
    }

    #region Private Methods

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            CustomerId = payment.CustomerId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            TransactionId = payment.TransactionId,
            ProcessedAt = payment.ProcessedAt,
            IdempotencyKey = payment.IdempotencyKey,
            ErrorMessage = payment.ErrorMessage
        };
    }

    private static RefundDto MapToRefundDto(Refund refund)
    {
        return new RefundDto
        {
            Id = refund.Id,
            PaymentId = refund.PaymentId,
            Amount = refund.Amount,
            Reason = refund.Reason,
            Status = refund.Status,
            TransactionId = refund.TransactionId,
            ProcessedAt = refund.ProcessedAt,
            IdempotencyKey = refund.IdempotencyKey,
            ErrorMessage = refund.ErrorMessage
        };
    }

    #endregion
}