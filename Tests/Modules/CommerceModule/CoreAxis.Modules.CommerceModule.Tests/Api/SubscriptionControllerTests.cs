using CoreAxis.Modules.CommerceModule.Api.Controllers;
using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Commands.Subscriptions;
using CoreAxis.Modules.CommerceModule.Application.Queries.Subscriptions;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.SharedKernel.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CoreAxis.Modules.CommerceModule.Tests.Api;

/// <summary>
/// Unit tests for SubscriptionController
/// </summary>
public class SubscriptionControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<SubscriptionController>> _loggerMock;
    private readonly SubscriptionController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testCustomerId = Guid.NewGuid();

    public SubscriptionControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<SubscriptionController>>();
        _controller = new SubscriptionController(_mediatorMock.Object, _loggerMock.Object);
        
        // Setup user context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new("customer_id", _testCustomerId.ToString()),
            new("permissions", "subscriptions.read"),
            new("permissions", "subscriptions.write"),
            new("permissions", "subscriptions.manage")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    #region GetSubscriptions Tests

    [Fact]
    public async Task GetSubscriptions_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var subscriptions = new List<Subscription>
        {
            new() 
            { 
                Id = Guid.NewGuid(), 
                CustomerId = _testCustomerId,
                PlanId = Guid.NewGuid(),
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-30),
                NextBillingDate = DateTime.UtcNow.AddDays(30),
                Amount = 29.99m,
                BillingCycle = BillingCycle.Monthly
            },
            new() 
            { 
                Id = Guid.NewGuid(), 
                CustomerId = _testCustomerId,
                PlanId = Guid.NewGuid(),
                Status = SubscriptionStatus.Paused,
                StartDate = DateTime.UtcNow.AddDays(-60),
                Amount = 99.99m,
                BillingCycle = BillingCycle.Yearly
            }
        };
        
        var pagedResult = new PagedResult<Subscription>(subscriptions, 2, 1, 10);
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetSubscriptionsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<Subscription>>.Success(pagedResult));

        // Act
        var result = await _controller.GetSubscriptions(1, 10, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<SubscriptionDto>>(okResult.Value);
        Assert.Equal(2, response.Items.Count());
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public async Task GetSubscriptions_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var subscriptions = new List<Subscription>
        {
            new() 
            { 
                Id = Guid.NewGuid(), 
                CustomerId = _testCustomerId,
                Status = SubscriptionStatus.Active,
                Amount = 29.99m
            }
        };
        
        var pagedResult = new PagedResult<Subscription>(subscriptions, 1, 1, 10);
        
        _mediatorMock.Setup(m => m.Send(It.Is<GetSubscriptionsQuery>(q => q.Status == SubscriptionStatus.Active), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<Subscription>>.Success(pagedResult));

        // Act
        var result = await _controller.GetSubscriptions(1, 10, SubscriptionStatus.Active, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<SubscriptionDto>>(okResult.Value);
        Assert.Single(response.Items);
        Assert.Equal(SubscriptionStatus.Active, response.Items.First().Status);
    }

    [Fact]
    public async Task GetSubscriptions_WhenMediatorFails_ReturnsInternalServerError()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetSubscriptionsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<Subscription>>.Failure("Database error"));

        // Act
        var result = await _controller.GetSubscriptions(1, 10, null, null);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetSubscription Tests

    [Fact]
    public async Task GetSubscription_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = new Subscription
        {
            Id = subscriptionId,
            CustomerId = _testCustomerId,
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-30),
            NextBillingDate = DateTime.UtcNow.AddDays(30),
            Amount = 49.99m,
            BillingCycle = BillingCycle.Monthly
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetSubscriptionQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Success(subscription));

        // Act
        var result = await _controller.GetSubscription(subscriptionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SubscriptionDto>(okResult.Value);
        Assert.Equal(subscriptionId, response.Id);
        Assert.Equal(49.99m, response.Amount);
        Assert.Equal(SubscriptionStatus.Active, response.Status);
    }

    [Fact]
    public async Task GetSubscription_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetSubscriptionQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Subscription not found"));

        // Act
        var result = await _controller.GetSubscription(subscriptionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Subscription not found", notFoundResult.Value?.ToString());
    }

    #endregion

    #region CreateSubscription Tests

    [Fact]
    public async Task CreateSubscription_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateSubscriptionDto
        {
            PlanId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            PaymentMethodId = Guid.NewGuid(),
            PromoCode = "SAVE10"
        };
        
        var createdSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = _testCustomerId,
            PlanId = createDto.PlanId,
            Status = SubscriptionStatus.Active,
            StartDate = createDto.StartDate,
            NextBillingDate = createDto.StartDate.AddMonths(1),
            Amount = 39.99m,
            BillingCycle = BillingCycle.Monthly
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Success(createdSubscription));

        // Act
        var result = await _controller.CreateSubscription(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<SubscriptionDto>(createdResult.Value);
        Assert.Equal(createDto.PlanId, response.PlanId);
        Assert.Equal(SubscriptionStatus.Active, response.Status);
        Assert.Equal("GetSubscription", createdResult.ActionName);
    }

    [Fact]
    public async Task CreateSubscription_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateSubscriptionDto
        {
            PlanId = Guid.Empty, // Invalid
            StartDate = DateTime.UtcNow.AddDays(-1), // Past date
            PaymentMethodId = Guid.Empty // Invalid
        };
        
        _controller.ModelState.AddModelError("PlanId", "Plan ID is required");
        _controller.ModelState.AddModelError("StartDate", "Start date cannot be in the past");
        _controller.ModelState.AddModelError("PaymentMethodId", "Payment method ID is required");

        // Act
        var result = await _controller.CreateSubscription(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateSubscription_WithInvalidPlan_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateSubscriptionDto
        {
            PlanId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            PaymentMethodId = Guid.NewGuid()
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Plan not found"));

        // Act
        var result = await _controller.CreateSubscription(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Plan not found", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task CreateSubscription_WithExistingActiveSubscription_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateSubscriptionDto
        {
            PlanId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            PaymentMethodId = Guid.NewGuid()
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Customer already has an active subscription for this plan"));

        // Act
        var result = await _controller.CreateSubscription(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Customer already has an active subscription", badRequestResult.Value?.ToString());
    }

    #endregion

    #region UpdateSubscription Tests

    [Fact]
    public async Task UpdateSubscription_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var updateDto = new UpdateSubscriptionDto
        {
            PlanId = Guid.NewGuid(),
            PaymentMethodId = Guid.NewGuid(),
            PromoCode = "UPGRADE20"
        };
        
        var updatedSubscription = new Subscription
        {
            Id = subscriptionId,
            CustomerId = _testCustomerId,
            PlanId = updateDto.PlanId,
            Status = SubscriptionStatus.Active,
            Amount = 59.99m,
            BillingCycle = BillingCycle.Monthly,
            UpdatedAt = DateTime.UtcNow
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Success(updatedSubscription));

        // Act
        var result = await _controller.UpdateSubscription(subscriptionId, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SubscriptionDto>(okResult.Value);
        Assert.Equal(subscriptionId, response.Id);
        Assert.Equal(updateDto.PlanId, response.PlanId);
        Assert.Equal(59.99m, response.Amount);
    }

    [Fact]
    public async Task UpdateSubscription_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var updateDto = new UpdateSubscriptionDto
        {
            PlanId = Guid.NewGuid()
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Subscription not found"));

        // Act
        var result = await _controller.UpdateSubscription(subscriptionId, updateDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Subscription not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task UpdateSubscription_WithCancelledSubscription_ReturnsBadRequest()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var updateDto = new UpdateSubscriptionDto
        {
            PlanId = Guid.NewGuid()
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Cannot update cancelled subscription"));

        // Act
        var result = await _controller.UpdateSubscription(subscriptionId, updateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Cannot update cancelled subscription", badRequestResult.Value?.ToString());
    }

    #endregion

    #region CancelSubscription Tests

    [Fact]
    public async Task CancelSubscription_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var cancelDto = new CancelSubscriptionDto
        {
            Reason = "No longer needed",
            CancelImmediately = false
        };
        
        var cancelledSubscription = new Subscription
        {
            Id = subscriptionId,
            CustomerId = _testCustomerId,
            Status = SubscriptionStatus.Cancelled,
            CancelledAt = DateTime.UtcNow,
            CancellationReason = cancelDto.Reason
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Success(cancelledSubscription));

        // Act
        var result = await _controller.CancelSubscription(subscriptionId, cancelDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SubscriptionDto>(okResult.Value);
        Assert.Equal(subscriptionId, response.Id);
        Assert.Equal(SubscriptionStatus.Cancelled, response.Status);
        Assert.Equal(cancelDto.Reason, response.CancellationReason);
    }

    [Fact]
    public async Task CancelSubscription_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var cancelDto = new CancelSubscriptionDto
        {
            Reason = "Test cancellation"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Subscription not found"));

        // Act
        var result = await _controller.CancelSubscription(subscriptionId, cancelDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Subscription not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task CancelSubscription_WithAlreadyCancelledSubscription_ReturnsBadRequest()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var cancelDto = new CancelSubscriptionDto
        {
            Reason = "Test cancellation"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Subscription is already cancelled"));

        // Act
        var result = await _controller.CancelSubscription(subscriptionId, cancelDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Subscription is already cancelled", badRequestResult.Value?.ToString());
    }

    #endregion

    #region PauseSubscription Tests

    [Fact]
    public async Task PauseSubscription_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var pauseDto = new PauseSubscriptionDto
        {
            Reason = "Temporary pause",
            PauseDuration = TimeSpan.FromDays(30)
        };
        
        var pausedSubscription = new Subscription
        {
            Id = subscriptionId,
            CustomerId = _testCustomerId,
            Status = SubscriptionStatus.Paused,
            PausedAt = DateTime.UtcNow,
            PauseReason = pauseDto.Reason,
            ResumeDate = DateTime.UtcNow.Add(pauseDto.PauseDuration)
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<PauseSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Success(pausedSubscription));

        // Act
        var result = await _controller.PauseSubscription(subscriptionId, pauseDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SubscriptionDto>(okResult.Value);
        Assert.Equal(subscriptionId, response.Id);
        Assert.Equal(SubscriptionStatus.Paused, response.Status);
        Assert.Equal(pauseDto.Reason, response.PauseReason);
    }

    [Fact]
    public async Task PauseSubscription_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var pauseDto = new PauseSubscriptionDto
        {
            Reason = "Test pause"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<PauseSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Subscription not found"));

        // Act
        var result = await _controller.PauseSubscription(subscriptionId, pauseDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Subscription not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task PauseSubscription_WithInactiveSubscription_ReturnsBadRequest()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var pauseDto = new PauseSubscriptionDto
        {
            Reason = "Test pause"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<PauseSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Only active subscriptions can be paused"));

        // Act
        var result = await _controller.PauseSubscription(subscriptionId, pauseDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Only active subscriptions can be paused", badRequestResult.Value?.ToString());
    }

    #endregion

    #region ResumeSubscription Tests

    [Fact]
    public async Task ResumeSubscription_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        
        var resumedSubscription = new Subscription
        {
            Id = subscriptionId,
            CustomerId = _testCustomerId,
            Status = SubscriptionStatus.Active,
            ResumedAt = DateTime.UtcNow,
            PausedAt = null,
            ResumeDate = null
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ResumeSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Success(resumedSubscription));

        // Act
        var result = await _controller.ResumeSubscription(subscriptionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SubscriptionDto>(okResult.Value);
        Assert.Equal(subscriptionId, response.Id);
        Assert.Equal(SubscriptionStatus.Active, response.Status);
    }

    [Fact]
    public async Task ResumeSubscription_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ResumeSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Subscription not found"));

        // Act
        var result = await _controller.ResumeSubscription(subscriptionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Subscription not found", notFoundResult.Value?.ToString());
    }

    [Fact]
    public async Task ResumeSubscription_WithNonPausedSubscription_ReturnsBadRequest()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ResumeSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Failure("Only paused subscriptions can be resumed"));

        // Act
        var result = await _controller.ResumeSubscription(subscriptionId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Only paused subscriptions can be resumed", badRequestResult.Value?.ToString());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateSubscription_WhenExceptionThrown_LogsErrorAndRethrows()
    {
        // Arrange
        var createDto = new CreateSubscriptionDto
        {
            PlanId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            PaymentMethodId = Guid.NewGuid()
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateSubscriptionCommand>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.CreateSubscription(createDto));
        
        // Verify logging occurred
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating subscription")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task SubscriptionLifecycle_CreatePauseResumeCancel_WorksCorrectly()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var createDto = new CreateSubscriptionDto
        {
            PlanId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            PaymentMethodId = Guid.NewGuid()
        };
        
        var pauseDto = new PauseSubscriptionDto
        {
            Reason = "Temporary pause",
            PauseDuration = TimeSpan.FromDays(30)
        };
        
        var cancelDto = new CancelSubscriptionDto
        {
            Reason = "No longer needed"
        };
        
        // Setup sequence of operations
        var createdSubscription = new Subscription { Id = subscriptionId, Status = SubscriptionStatus.Active };
        var pausedSubscription = new Subscription { Id = subscriptionId, Status = SubscriptionStatus.Paused };
        var resumedSubscription = new Subscription { Id = subscriptionId, Status = SubscriptionStatus.Active };
        var cancelledSubscription = new Subscription { Id = subscriptionId, Status = SubscriptionStatus.Cancelled };
        
        _mediatorMock.SetupSequence(m => m.Send(It.IsAny<IRequest<Result<Subscription>>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Subscription>.Success(createdSubscription))
                    .ReturnsAsync(Result<Subscription>.Success(pausedSubscription))
                    .ReturnsAsync(Result<Subscription>.Success(resumedSubscription))
                    .ReturnsAsync(Result<Subscription>.Success(cancelledSubscription));

        // Act & Assert
        // 1. Create subscription
        var createResult = await _controller.CreateSubscription(createDto);
        var createdResult = Assert.IsType<CreatedAtActionResult>(createResult);
        var createdResponse = Assert.IsType<SubscriptionDto>(createdResult.Value);
        Assert.Equal(SubscriptionStatus.Active, createdResponse.Status);
        
        // 2. Pause subscription
        var pauseResult = await _controller.PauseSubscription(subscriptionId, pauseDto);
        var pauseOkResult = Assert.IsType<OkObjectResult>(pauseResult);
        var pauseResponse = Assert.IsType<SubscriptionDto>(pauseOkResult.Value);
        Assert.Equal(SubscriptionStatus.Paused, pauseResponse.Status);
        
        // 3. Resume subscription
        var resumeResult = await _controller.ResumeSubscription(subscriptionId);
        var resumeOkResult = Assert.IsType<OkObjectResult>(resumeResult);
        var resumeResponse = Assert.IsType<SubscriptionDto>(resumeOkResult.Value);
        Assert.Equal(SubscriptionStatus.Active, resumeResponse.Status);
        
        // 4. Cancel subscription
        var cancelResult = await _controller.CancelSubscription(subscriptionId, cancelDto);
        var cancelOkResult = Assert.IsType<OkObjectResult>(cancelResult);
        var cancelResponse = Assert.IsType<SubscriptionDto>(cancelOkResult.Value);
        Assert.Equal(SubscriptionStatus.Cancelled, cancelResponse.Status);
    }

    #endregion
}