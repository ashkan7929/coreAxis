using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.AuthModule.API.Authz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CoreAxis.Modules.CommerceModule.Api.Controllers;

/// <summary>
/// Controller for managing subscription operations
/// </summary>
[ApiController]
[Route("api/v1/commerce/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<SubscriptionController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionController"/> class
    /// </summary>
    /// <param name="subscriptionService">The subscription service</param>
    /// <param name="paymentService">The payment service</param>
    /// <param name="logger">The logger</param>
    public SubscriptionController(
        ISubscriptionService subscriptionService,
        IPaymentService paymentService,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets subscriptions with optional filtering
    /// </summary>
    /// <param name="customerId">Optional customer ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="planId">Optional plan ID filter</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>List of subscriptions</returns>
    [HttpGet]
    [HasPermission("subscriptions", "read")]
    public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetSubscriptions(
        [FromQuery] Guid? customerId = null,
        [FromQuery] SubscriptionStatus? status = null,
        [FromQuery] Guid? planId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            _logger.LogInformation("Getting subscriptions with filters: CustomerId={CustomerId}, Status={Status}, PlanId={PlanId}, Page={Page}, PageSize={PageSize}",
                customerId, status, planId, page, pageSize);

            var subscriptions = await _subscriptionService.GetSubscriptionsAsync(
                customerId, status, planId, page, pageSize);

            var dtos = subscriptions.Select(MapToDto).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions");
            return StatusCode(500, "An error occurred while retrieving subscriptions");
        }
    }

    /// <summary>
    /// Gets a specific subscription by ID
    /// </summary>
    /// <param name="id">The subscription ID</param>
    /// <returns>The subscription</returns>
    [HttpGet("{id:guid}")]
    [HasPermission("subscriptions", "read")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting subscription with ID: {Id}", id);

            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
            {
                return NotFound($"Subscription with ID {id} not found");
            }

            return Ok(MapToDto(subscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the subscription");
        }
    }

    /// <summary>
    /// Creates a new subscription
    /// </summary>
    /// <param name="createDto">The subscription creation data</param>
    /// <returns>The created subscription</returns>
    [HttpPost]
    [HasPermission("subscriptions", "create")]
    public async Task<ActionResult<SubscriptionDto>> CreateSubscription([FromBody] CreateSubscriptionDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} creating new subscription for customer: {CustomerId}, Plan: {PlanId}", 
                userId, createDto.CustomerId, createDto.PlanId);

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                CustomerId = createDto.CustomerId,
                PlanId = createDto.PlanId,
                Status = SubscriptionStatus.Active,
                StartDate = createDto.StartDate ?? DateTime.UtcNow,
                NextBillingDate = createDto.StartDate?.AddMonths(1) ?? DateTime.UtcNow.AddMonths(1),
                Amount = createDto.Amount,
                BillingCycle = createDto.BillingCycle,
                CreatedAt = DateTime.UtcNow
            };

            var createdSubscription = await _subscriptionService.CreateSubscriptionAsync(subscription);

            _logger.LogInformation("Successfully created subscription with ID: {Id}", createdSubscription.Id);

            return CreatedAtAction(
                nameof(GetSubscription),
                new { id = createdSubscription.Id },
                MapToDto(createdSubscription));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating subscription");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return StatusCode(500, "An error occurred while creating the subscription");
        }
    }

    /// <summary>
    /// Updates an existing subscription
    /// </summary>
    /// <param name="id">The subscription ID</param>
    /// <param name="updateDto">The subscription update data</param>
    /// <returns>The updated subscription</returns>
    [HttpPut("{id:guid}")]
    [HasPermission("subscriptions", "update")]
    public async Task<ActionResult<SubscriptionDto>> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} updating subscription with ID: {Id}", userId, id);

            var existingSubscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (existingSubscription == null)
            {
                return NotFound($"Subscription with ID {id} not found");
            }

            // Update allowed fields
            if (updateDto.PlanId.HasValue)
                existingSubscription.PlanId = updateDto.PlanId.Value;
            if (updateDto.Amount.HasValue)
                existingSubscription.Amount = updateDto.Amount.Value;
            if (updateDto.BillingCycle.HasValue)
                existingSubscription.BillingCycle = updateDto.BillingCycle.Value;
            if (updateDto.NextBillingDate.HasValue)
                existingSubscription.NextBillingDate = updateDto.NextBillingDate.Value;

            existingSubscription.UpdatedAt = DateTime.UtcNow;

            var updatedSubscription = await _subscriptionService.UpdateSubscriptionAsync(existingSubscription);

            _logger.LogInformation("Successfully updated subscription with ID: {Id}", id);

            return Ok(MapToDto(updatedSubscription));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when updating subscription with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription with ID: {Id}", id);
            return StatusCode(500, "An error occurred while updating the subscription");
        }
    }

    /// <summary>
    /// Cancels a subscription
    /// </summary>
    /// <param name="id">The subscription ID</param>
    /// <param name="cancelDto">The cancellation data</param>
    /// <returns>The cancelled subscription</returns>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission("subscriptions", "cancel")]
    public async Task<ActionResult<SubscriptionDto>> CancelSubscription(Guid id, [FromBody] CancelSubscriptionDto cancelDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} cancelling subscription with ID: {Id}, Reason: {Reason}", 
                userId, id, cancelDto.Reason);

            var cancelledSubscription = await _subscriptionService.CancelSubscriptionAsync(
                id, cancelDto.Reason, cancelDto.CancelImmediately);
            
            if (cancelledSubscription == null)
            {
                return NotFound($"Subscription with ID {id} not found");
            }

            _logger.LogInformation("Successfully cancelled subscription with ID: {Id}", id);

            return Ok(MapToDto(cancelledSubscription));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when cancelling subscription with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription with ID: {Id}", id);
            return StatusCode(500, "An error occurred while cancelling the subscription");
        }
    }

    /// <summary>
    /// Pauses a subscription
    /// </summary>
    /// <param name="id">The subscription ID</param>
    /// <param name="pauseDto">The pause data</param>
    /// <returns>The paused subscription</returns>
    [HttpPost("{id:guid}/pause")]
    [HasPermission("subscriptions", "pause")]
    public async Task<ActionResult<SubscriptionDto>> PauseSubscription(Guid id, [FromBody] PauseSubscriptionDto pauseDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} pausing subscription with ID: {Id}, Until: {PauseUntil}", 
                userId, id, pauseDto.PauseUntil);

            var pausedSubscription = await _subscriptionService.PauseSubscriptionAsync(id, pauseDto.PauseUntil);
            if (pausedSubscription == null)
            {
                return NotFound($"Subscription with ID {id} not found");
            }

            _logger.LogInformation("Successfully paused subscription with ID: {Id}", id);

            return Ok(MapToDto(pausedSubscription));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when pausing subscription with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing subscription with ID: {Id}", id);
            return StatusCode(500, "An error occurred while pausing the subscription");
        }
    }

    /// <summary>
    /// Resumes a paused subscription
    /// </summary>
    /// <param name="id">The subscription ID</param>
    /// <returns>The resumed subscription</returns>
    [HttpPost("{id:guid}/resume")]
    [HasPermission("subscriptions", "resume")]
    public async Task<ActionResult<SubscriptionDto>> ResumeSubscription(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} resuming subscription with ID: {Id}", userId, id);

            var resumedSubscription = await _subscriptionService.ResumeSubscriptionAsync(id);
            if (resumedSubscription == null)
            {
                return NotFound($"Subscription with ID {id} not found");
            }

            _logger.LogInformation("Successfully resumed subscription with ID: {Id}", id);

            return Ok(MapToDto(resumedSubscription));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when resuming subscription with ID: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming subscription with ID: {Id}", id);
            return StatusCode(500, "An error occurred while resuming the subscription");
        }
    }

    /// <summary>
    /// Processes billing for a subscription
    /// </summary>
    /// <param name="id">The subscription ID</param>
    /// <returns>The billing result</returns>
    [HttpPost("{id:guid}/bill")]
    [HasPermission("subscriptions", "bill")]
    public async Task<ActionResult<PaymentDto>> ProcessSubscriptionBilling(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} processing billing for subscription: {Id}", userId, id);

            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
            {
                return NotFound($"Subscription with ID {id} not found");
            }

            if (subscription.Status != SubscriptionStatus.Active)
            {
                return BadRequest($"Cannot bill subscription in {subscription.Status} status");
            }

            var billingResult = await _subscriptionService.ProcessBillingAsync(id);

            _logger.LogInformation("Successfully processed billing for subscription: {Id}, Payment: {PaymentId}", 
                id, billingResult.Id);

            return Ok(new PaymentDto
            {
                Id = billingResult.Id,
                OrderId = Guid.Empty, // Subscription payments don't have orders
                CustomerId = subscription.CustomerId,
                Amount = billingResult.Amount,
                PaymentMethod = billingResult.PaymentMethod,
                Status = billingResult.Status,
                TransactionId = billingResult.TransactionId,
                ProcessedAt = billingResult.ProcessedAt,
                IdempotencyKey = billingResult.IdempotencyKey,
                ErrorMessage = billingResult.ErrorMessage
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when processing billing for subscription: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing billing for subscription: {Id}", id);
            return StatusCode(500, "An error occurred while processing subscription billing");
        }
    }

    #region Private Methods

    private static SubscriptionDto MapToDto(Subscription subscription)
    {
        return new SubscriptionDto
        {
            Id = subscription.Id,
            CustomerId = subscription.CustomerId,
            PlanId = subscription.PlanId,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            NextBillingDate = subscription.NextBillingDate,
            Amount = subscription.Amount,
            BillingCycle = subscription.BillingCycle,
            CancellationReason = subscription.CancellationReason,
            PauseUntil = subscription.PauseUntil,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };
    }

    #endregion
}