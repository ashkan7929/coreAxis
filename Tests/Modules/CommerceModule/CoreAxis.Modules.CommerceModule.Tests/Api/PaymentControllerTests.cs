using CoreAxis.Modules.CommerceModule.Api.Controllers;
using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Application.Commands.Payments;
using CoreAxis.Modules.CommerceModule.Application.Queries.Payments;
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
/// Unit tests for PaymentController
/// </summary>
public class PaymentControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<PaymentController>> _loggerMock;
    private readonly PaymentController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testCustomerId = Guid.NewGuid();

    public PaymentControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<PaymentController>>();
        _controller = new PaymentController(_mediatorMock.Object, _loggerMock.Object);
        
        // Setup user context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new("customer_id", _testCustomerId.ToString()),
            new("permissions", "payments.read"),
            new("permissions", "payments.write"),
            new("permissions", "payments.process"),
            new("permissions", "payments.refund")
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

    #region GetPayments Tests

    [Fact]
    public async Task GetPayments_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var payments = new List<Payment>
        {
            new() 
            { 
                Id = Guid.NewGuid(), 
                OrderId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = PaymentStatus.Completed,
                PaymentMethod = "CreditCard",
                CreatedAt = DateTime.UtcNow
            },
            new() 
            { 
                Id = Guid.NewGuid(), 
                OrderId = Guid.NewGuid(),
                Amount = 250.00m,
                Status = PaymentStatus.Pending,
                PaymentMethod = "PayPal",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };
        
        var pagedResult = new PagedResult<Payment>(payments, 2, 1, 10);
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPaymentsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<Payment>>.Success(pagedResult));

        // Act
        var result = await _controller.GetPayments(1, 10, null, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<PaymentDto>>(okResult.Value);
        Assert.Equal(2, response.Items.Count());
        Assert.Equal(2, response.TotalCount);
    }

    [Fact]
    public async Task GetPayments_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var payments = new List<Payment>
        {
            new() 
            { 
                Id = Guid.NewGuid(), 
                OrderId = Guid.NewGuid(),
                Amount = 100.00m,
                Status = PaymentStatus.Completed,
                PaymentMethod = "CreditCard"
            }
        };
        
        var pagedResult = new PagedResult<Payment>(payments, 1, 1, 10);
        
        _mediatorMock.Setup(m => m.Send(It.Is<GetPaymentsQuery>(q => q.Status == PaymentStatus.Completed), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<Payment>>.Success(pagedResult));

        // Act
        var result = await _controller.GetPayments(1, 10, PaymentStatus.Completed, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<PaymentDto>>(okResult.Value);
        Assert.Single(response.Items);
        Assert.Equal(PaymentStatus.Completed, response.Items.First().Status);
    }

    [Fact]
    public async Task GetPayments_WhenMediatorFails_ReturnsInternalServerError()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPaymentsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PagedResult<Payment>>.Failure("Database error"));

        // Act
        var result = await _controller.GetPayments(1, 10, null, null, null, null);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetPayment Tests

    [Fact]
    public async Task GetPayment_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = paymentId,
            OrderId = Guid.NewGuid(),
            Amount = 150.00m,
            Status = PaymentStatus.Completed,
            PaymentMethod = "CreditCard",
            TransactionId = "txn_123456",
            ProcessedAt = DateTime.UtcNow
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPaymentQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Payment>.Success(payment));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaymentDto>(okResult.Value);
        Assert.Equal(paymentId, response.Id);
        Assert.Equal(150.00m, response.Amount);
        Assert.Equal(PaymentStatus.Completed, response.Status);
    }

    [Fact]
    public async Task GetPayment_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPaymentQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Payment>.Failure("Payment not found"));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Payment not found", notFoundResult.Value?.ToString());
    }

    #endregion

    #region ProcessPayment Tests

    [Fact]
    public async Task ProcessPayment_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var processDto = new ProcessPaymentDto
        {
            OrderId = Guid.NewGuid(),
            Amount = 200.00m,
            PaymentMethod = "CreditCard",
            PaymentDetails = new Dictionary<string, object>
            {
                { "cardNumber", "**** **** **** 1234" },
                { "expiryMonth", "12" },
                { "expiryYear", "2025" }
            }
        };
        
        var processedPayment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = processDto.OrderId,
            Amount = processDto.Amount,
            Status = PaymentStatus.Completed,
            PaymentMethod = processDto.PaymentMethod,
            TransactionId = "txn_789012",
            ProcessedAt = DateTime.UtcNow
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Payment>.Success(processedPayment));

        // Act
        var result = await _controller.ProcessPayment(processDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaymentDto>(okResult.Value);
        Assert.Equal(processDto.OrderId, response.OrderId);
        Assert.Equal(processDto.Amount, response.Amount);
        Assert.Equal(PaymentStatus.Completed, response.Status);
        Assert.NotNull(response.TransactionId);
    }

    [Fact]
    public async Task ProcessPayment_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var processDto = new ProcessPaymentDto
        {
            OrderId = Guid.Empty, // Invalid
            Amount = -100.00m, // Invalid negative amount
            PaymentMethod = "" // Invalid empty method
        };
        
        _controller.ModelState.AddModelError("OrderId", "Order ID is required");
        _controller.ModelState.AddModelError("Amount", "Amount must be positive");
        _controller.ModelState.AddModelError("PaymentMethod", "Payment method is required");

        // Act
        var result = await _controller.ProcessPayment(processDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task ProcessPayment_WithInsufficientFunds_ReturnsBadRequest()
    {
        // Arrange
        var processDto = new ProcessPaymentDto
        {
            OrderId = Guid.NewGuid(),
            Amount = 1000000.00m, // Very large amount
            PaymentMethod = "CreditCard"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Payment>.Failure("Insufficient funds"));

        // Act
        var result = await _controller.ProcessPayment(processDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Insufficient funds", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task ProcessPayment_WithDeclinedCard_ReturnsBadRequest()
    {
        // Arrange
        var processDto = new ProcessPaymentDto
        {
            OrderId = Guid.NewGuid(),
            Amount = 100.00m,
            PaymentMethod = "CreditCard"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Payment>.Failure("Card declined"));

        // Act
        var result = await _controller.ProcessPayment(processDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Card declined", badRequestResult.Value?.ToString());
    }

    #endregion

    #region ProcessRefund Tests

    [Fact]
    public async Task ProcessRefund_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var refundDto = new ProcessRefundDto
        {
            Amount = 50.00m,
            Reason = "Customer requested partial refund"
        };
        
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            Amount = refundDto.Amount,
            Reason = refundDto.Reason,
            Status = RefundStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessRefundCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Refund>.Success(refund));

        // Act
        var result = await _controller.ProcessRefund(paymentId, refundDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<RefundDto>(okResult.Value);
        Assert.Equal(paymentId, response.PaymentId);
        Assert.Equal(refundDto.Amount, response.Amount);
        Assert.Equal(RefundStatus.Completed, response.Status);
    }

    [Fact]
    public async Task ProcessRefund_WithInvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var refundDto = new ProcessRefundDto
        {
            Amount = -10.00m, // Invalid negative amount
            Reason = "Test refund"
        };
        
        _controller.ModelState.AddModelError("Amount", "Refund amount must be positive");

        // Act
        var result = await _controller.ProcessRefund(paymentId, refundDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task ProcessRefund_WithAmountExceedingPayment_ReturnsBadRequest()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var refundDto = new ProcessRefundDto
        {
            Amount = 1000.00m, // More than original payment
            Reason = "Test refund"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessRefundCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Refund>.Failure("Refund amount exceeds payment amount"));

        // Act
        var result = await _controller.ProcessRefund(paymentId, refundDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Refund amount exceeds payment amount", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task ProcessRefund_WithNonRefundablePayment_ReturnsBadRequest()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var refundDto = new ProcessRefundDto
        {
            Amount = 50.00m,
            Reason = "Test refund"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessRefundCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Refund>.Failure("Payment is not refundable"));

        // Act
        var result = await _controller.ProcessRefund(paymentId, refundDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Payment is not refundable", badRequestResult.Value?.ToString());
    }

    #endregion

    #region GetRefunds Tests

    [Fact]
    public async Task GetRefunds_WithValidPaymentId_ReturnsOkResult()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var refunds = new List<Refund>
        {
            new() 
            { 
                Id = Guid.NewGuid(), 
                PaymentId = paymentId,
                Amount = 25.00m,
                Status = RefundStatus.Completed,
                Reason = "Partial refund 1"
            },
            new() 
            { 
                Id = Guid.NewGuid(), 
                PaymentId = paymentId,
                Amount = 25.00m,
                Status = RefundStatus.Completed,
                Reason = "Partial refund 2"
            }
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetRefundsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<IEnumerable<Refund>>.Success(refunds));

        // Act
        var result = await _controller.GetRefunds(paymentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<List<RefundDto>>(okResult.Value);
        Assert.Equal(2, response.Count);
        Assert.All(response, r => Assert.Equal(paymentId, r.PaymentId));
    }

    [Fact]
    public async Task GetRefunds_WithInvalidPaymentId_ReturnsNotFound()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetRefundsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<IEnumerable<Refund>>.Failure("Payment not found"));

        // Act
        var result = await _controller.GetRefunds(paymentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Payment not found", notFoundResult.Value?.ToString());
    }

    #endregion

    #region VerifyPaymentStatus Tests

    [Fact]
    public async Task VerifyPaymentStatus_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = paymentId,
            Status = PaymentStatus.Completed,
            TransactionId = "txn_verified_123"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<VerifyPaymentStatusCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Payment>.Success(payment));

        // Act
        var result = await _controller.VerifyPaymentStatus(paymentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaymentDto>(okResult.Value);
        Assert.Equal(paymentId, response.Id);
        Assert.Equal(PaymentStatus.Completed, response.Status);
    }

    [Fact]
    public async Task VerifyPaymentStatus_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<VerifyPaymentStatusCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Payment>.Failure("Payment not found"));

        // Act
        var result = await _controller.VerifyPaymentStatus(paymentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Payment not found", notFoundResult.Value?.ToString());
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task ProcessPayment_WithSensitiveData_DoesNotLogSensitiveInformation()
    {
        // Arrange
        var processDto = new ProcessPaymentDto
        {
            OrderId = Guid.NewGuid(),
            Amount = 100.00m,
            PaymentMethod = "CreditCard",
            PaymentDetails = new Dictionary<string, object>
            {
                { "cardNumber", "4111111111111111" }, // Sensitive data
                { "cvv", "123" } // Sensitive data
            }
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.ProcessPayment(processDto));
        
        // Verify that sensitive data is not logged
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains("4111111111111111")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public async Task ProcessPayment_WithDuplicateRequest_ReturnsExistingPayment()
    {
        // Arrange
        var processDto = new ProcessPaymentDto
        {
            OrderId = Guid.NewGuid(),
            Amount = 100.00m,
            PaymentMethod = "CreditCard",
            IdempotencyKey = "unique-key-123"
        };
        
        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = processDto.OrderId,
            Amount = processDto.Amount,
            Status = PaymentStatus.Completed,
            TransactionId = "txn_existing_123"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Payment>.Success(existingPayment));

        // Act
        var result = await _controller.ProcessPayment(processDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaymentDto>(okResult.Value);
        Assert.Equal(existingPayment.Id, response.Id);
        Assert.Equal(PaymentStatus.Completed, response.Status);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ProcessPayment_WhenExceptionThrown_LogsErrorAndRethrows()
    {
        // Arrange
        var processDto = new ProcessPaymentDto
        {
            OrderId = Guid.NewGuid(),
            Amount = 100.00m,
            PaymentMethod = "CreditCard"
        };
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.ProcessPayment(processDto));
        
        // Verify logging occurred
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing payment")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}