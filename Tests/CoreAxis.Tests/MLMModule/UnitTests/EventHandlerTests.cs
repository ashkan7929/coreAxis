using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Infrastructure.EventHandlers;
using CoreAxis.Modules.WalletModule.IntegrationEvents;
using CoreAxis.SharedKernel.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class EventHandlerTests
{
    private readonly Mock<ICommissionCalculationService> _commissionServiceMock;
    private readonly Mock<ILogger<PaymentConfirmedEventHandler>> _loggerMock;
    private readonly PaymentConfirmedEventHandler _eventHandler;

    public EventHandlerTests()
    {
        _commissionServiceMock = new Mock<ICommissionCalculationService>();
        _loggerMock = new Mock<ILogger<PaymentConfirmedEventHandler>>();
        _eventHandler = new PaymentConfirmedEventHandler(
            _commissionServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_PaymentConfirmedEvent_ShouldCallCommissionCalculationService()
    {
        // Arrange
        var paymentConfirmedEvent = new PaymentConfirmedEvent
        {
            PaymentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 1000m,
            ProductId = Guid.NewGuid(),
            ConfirmedAt = DateTime.UtcNow
        };

        _commissionServiceMock
            .Setup(x => x.ProcessPaymentConfirmedAsync(
                paymentConfirmedEvent.PaymentId,
                paymentConfirmedEvent.UserId,
                paymentConfirmedEvent.Amount,
                paymentConfirmedEvent.ProductId
            ))
            .Returns(Task.CompletedTask);

        // Act
        await _eventHandler.Handle(paymentConfirmedEvent, CancellationToken.None);

        // Assert
        _commissionServiceMock.Verify(
            x => x.ProcessPaymentConfirmedAsync(
                paymentConfirmedEvent.PaymentId,
                paymentConfirmedEvent.UserId,
                paymentConfirmedEvent.Amount,
                paymentConfirmedEvent.ProductId
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_PaymentConfirmedEvent_WhenServiceThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var paymentConfirmedEvent = new PaymentConfirmedEvent
        {
            PaymentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 1000m,
            ProductId = Guid.NewGuid(),
            ConfirmedAt = DateTime.UtcNow
        };

        var expectedException = new InvalidOperationException("Test exception");
        
        _commissionServiceMock
            .Setup(x => x.ProcessPaymentConfirmedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<Guid>()
            ))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _eventHandler.Handle(paymentConfirmedEvent, CancellationToken.None)
        );

        Assert.Equal(expectedException.Message, actualException.Message);
        
        // Verify that error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error processing payment confirmed event")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_PaymentConfirmedEvent_WithZeroAmount_ShouldStillCallService()
    {
        // Arrange
        var paymentConfirmedEvent = new PaymentConfirmedEvent
        {
            PaymentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 0m, // Zero amount
            ProductId = Guid.NewGuid(),
            ConfirmedAt = DateTime.UtcNow
        };

        _commissionServiceMock
            .Setup(x => x.ProcessPaymentConfirmedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<Guid>()
            ))
            .Returns(Task.CompletedTask);

        // Act
        await _eventHandler.Handle(paymentConfirmedEvent, CancellationToken.None);

        // Assert
        _commissionServiceMock.Verify(
            x => x.ProcessPaymentConfirmedAsync(
                paymentConfirmedEvent.PaymentId,
                paymentConfirmedEvent.UserId,
                0m,
                paymentConfirmedEvent.ProductId
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_PaymentConfirmedEvent_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var paymentConfirmedEvent = new PaymentConfirmedEvent
        {
            PaymentId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 1000m,
            ProductId = Guid.NewGuid(),
            ConfirmedAt = DateTime.UtcNow
        };

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately

        _commissionServiceMock
            .Setup(x => x.ProcessPaymentConfirmedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<Guid>()
            ))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _eventHandler.Handle(paymentConfirmedEvent, cancellationTokenSource.Token)
        );
    }
}

public class CommissionApprovedEventHandlerTests
{
    private readonly Mock<ILogger<CommissionApprovedEventHandler>> _loggerMock;
    private readonly CommissionApprovedEventHandler _eventHandler;

    public CommissionApprovedEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<CommissionApprovedEventHandler>>();
        _eventHandler = new CommissionApprovedEventHandler(_loggerMock.Object);
    }

    [Fact]
    public async Task Handle_CommissionApprovedEvent_ShouldLogInformation()
    {
        // Arrange
        var commissionApprovedEvent = new CommissionApprovedEvent(
            Guid.NewGuid(), // CommissionId
            Guid.NewGuid(), // UserId
            100m,           // Amount
            Guid.NewGuid(), // SourcePaymentId
            Guid.NewGuid(), // ApprovedBy
            DateTime.UtcNow // ApprovedAt
        );

        // Act
        await _eventHandler.Handle(commissionApprovedEvent, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Commission approved")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_CommissionApprovedEvent_ShouldCompleteSuccessfully()
    {
        // Arrange
        var commissionApprovedEvent = new CommissionApprovedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        // Act
        var task = _eventHandler.Handle(commissionApprovedEvent, CancellationToken.None);

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        await task; // Should not throw
    }

    [Fact]
    public async Task Handle_CommissionApprovedEvent_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var commissionApprovedEvent = new CommissionApprovedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _eventHandler.Handle(commissionApprovedEvent, cancellationTokenSource.Token)
        );
    }
}