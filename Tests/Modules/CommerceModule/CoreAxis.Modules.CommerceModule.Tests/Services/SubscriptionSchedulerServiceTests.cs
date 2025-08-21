using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Events;
using CoreAxis.Shared.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;

namespace CoreAxis.Modules.CommerceModule.Tests.Services;

public class SubscriptionSchedulerServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<ICommerceDbContext> _mockContext;
    private readonly Mock<ISubscriptionService> _mockSubscriptionService;
    private readonly Mock<IDomainEventDispatcher> _mockEventDispatcher;
    private readonly Mock<ILogger<SubscriptionSchedulerService>> _mockLogger;
    private readonly SubscriptionSchedulerOptions _options;
    private readonly SubscriptionSchedulerService _service;

    public SubscriptionSchedulerServiceTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockContext = new Mock<ICommerceDbContext>();
        _mockSubscriptionService = new Mock<ISubscriptionService>();
        _mockEventDispatcher = new Mock<IDomainEventDispatcher>();
        _mockLogger = new Mock<ILogger<SubscriptionSchedulerService>>();
        
        _options = new SubscriptionSchedulerOptions
        {
            ProcessingIntervalMinutes = 60,
            BatchSize = 100,
            MaxRetryAttempts = 3,
            RetryDelayMinutes = 30,
            GracePeriodDays = 7,
            CleanupRetentionDays = 90
        };
        
        var mockOptions = new Mock<IOptions<SubscriptionSchedulerOptions>>();
        mockOptions.Setup(o => o.Value).Returns(_options);
        
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
        
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(ICommerceDbContext))).Returns(_mockContext.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(ISubscriptionService))).Returns(_mockSubscriptionService.Object);
        _mockServiceProvider.Setup(p => p.GetService(typeof(IDomainEventDispatcher))).Returns(_mockEventDispatcher.Object);
        
        _service = new SubscriptionSchedulerService(
            mockServiceScopeFactory.Object,
            mockOptions.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessDueRenewals_WithDueSubscriptions_ShouldProcessRenewals()
    {
        // Arrange
        var dueDate = DateTime.UtcNow.Date;
        var subscription1 = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            NextBillingDate = dueDate,
            Plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Monthly Plan",
                Price = 29.99m,
                BillingCycle = BillingCycle.Monthly
            },
            Customer = new Customer
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com"
            }
        };
        
        var subscription2 = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            NextBillingDate = dueDate,
            Plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Annual Plan",
                Price = 299.99m,
                BillingCycle = BillingCycle.Yearly
            },
            Customer = new Customer
            {
                Id = Guid.NewGuid(),
                Email = "test2@example.com"
            }
        };

        var subscriptions = new List<Subscription> { subscription1, subscription2 }.AsQueryable();
        var mockSubscriptionSet = new Mock<DbSet<Subscription>>();
        
        _mockContext.Setup(c => c.Subscriptions).Returns(mockSubscriptionSet.Object);
        
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Provider).Returns(subscriptions.Provider);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Expression).Returns(subscriptions.Expression);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.ElementType).Returns(subscriptions.ElementType);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.GetEnumerator()).Returns(subscriptions.GetEnumerator());

        _mockSubscriptionService.Setup(s => s.RenewSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.ProcessDueRenewalsAsync(CancellationToken.None);

        // Assert
        _mockSubscriptionService.Verify(
            s => s.RenewSubscriptionAsync(subscription1.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockSubscriptionService.Verify(
            s => s.RenewSubscriptionAsync(subscription2.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDueRenewals_WithRenewalFailure_ShouldLogAndContinue()
    {
        // Arrange
        var dueDate = DateTime.UtcNow.Date;
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            NextBillingDate = dueDate,
            Plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Monthly Plan",
                Price = 29.99m,
                BillingCycle = BillingCycle.Monthly
            },
            Customer = new Customer
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com"
            }
        };

        var subscriptions = new List<Subscription> { subscription }.AsQueryable();
        var mockSubscriptionSet = new Mock<DbSet<Subscription>>();
        
        _mockContext.Setup(c => c.Subscriptions).Returns(mockSubscriptionSet.Object);
        
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Provider).Returns(subscriptions.Provider);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Expression).Returns(subscriptions.Expression);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.ElementType).Returns(subscriptions.ElementType);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.GetEnumerator()).Returns(subscriptions.GetEnumerator());

        _mockSubscriptionService.Setup(s => s.RenewSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Payment failed"));

        // Act
        await _service.ProcessDueRenewalsAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to renew subscription")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTrialExpirations_WithExpiredTrials_ShouldProcessExpirations()
    {
        // Arrange
        var expiredDate = DateTime.UtcNow.AddDays(-1);
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Trial,
            TrialEndDate = expiredDate,
            Plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Trial Plan",
                Price = 0m,
                BillingCycle = BillingCycle.Monthly
            },
            Customer = new Customer
            {
                Id = Guid.NewGuid(),
                Email = "trial@example.com"
            }
        };

        var subscriptions = new List<Subscription> { subscription }.AsQueryable();
        var mockSubscriptionSet = new Mock<DbSet<Subscription>>();
        
        _mockContext.Setup(c => c.Subscriptions).Returns(mockSubscriptionSet.Object);
        
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Provider).Returns(subscriptions.Provider);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Expression).Returns(subscriptions.Expression);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.ElementType).Returns(subscriptions.ElementType);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.GetEnumerator()).Returns(subscriptions.GetEnumerator());

        _mockSubscriptionService.Setup(s => s.ExpireTrialAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessTrialExpirationsAsync(CancellationToken.None);

        // Assert
        _mockSubscriptionService.Verify(
            s => s.ExpireTrialAsync(subscription.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessFailedPayments_WithFailedPayments_ShouldProcessRetries()
    {
        // Arrange
        var failedDate = DateTime.UtcNow.AddHours(-1);
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.PastDue,
            LastPaymentAttempt = failedDate,
            PaymentRetryCount = 1,
            Plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Monthly Plan",
                Price = 29.99m,
                BillingCycle = BillingCycle.Monthly
            },
            Customer = new Customer
            {
                Id = Guid.NewGuid(),
                Email = "pastdue@example.com"
            }
        };

        var subscriptions = new List<Subscription> { subscription }.AsQueryable();
        var mockSubscriptionSet = new Mock<DbSet<Subscription>>();
        
        _mockContext.Setup(c => c.Subscriptions).Returns(mockSubscriptionSet.Object);
        
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Provider).Returns(subscriptions.Provider);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Expression).Returns(subscriptions.Expression);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.ElementType).Returns(subscriptions.ElementType);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.GetEnumerator()).Returns(subscriptions.GetEnumerator());

        _mockSubscriptionService.Setup(s => s.RetryPaymentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.ProcessFailedPaymentsAsync(CancellationToken.None);

        // Assert
        _mockSubscriptionService.Verify(
            s => s.RetryPaymentAsync(subscription.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessFailedPayments_WithMaxRetriesReached_ShouldCancelSubscription()
    {
        // Arrange
        var failedDate = DateTime.UtcNow.AddHours(-1);
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.PastDue,
            LastPaymentAttempt = failedDate,
            PaymentRetryCount = _options.MaxRetryAttempts, // Max retries reached
            Plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Monthly Plan",
                Price = 29.99m,
                BillingCycle = BillingCycle.Monthly
            },
            Customer = new Customer
            {
                Id = Guid.NewGuid(),
                Email = "maxretries@example.com"
            }
        };

        var subscriptions = new List<Subscription> { subscription }.AsQueryable();
        var mockSubscriptionSet = new Mock<DbSet<Subscription>>();
        
        _mockContext.Setup(c => c.Subscriptions).Returns(mockSubscriptionSet.Object);
        
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Provider).Returns(subscriptions.Provider);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Expression).Returns(subscriptions.Expression);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.ElementType).Returns(subscriptions.ElementType);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.GetEnumerator()).Returns(subscriptions.GetEnumerator());

        _mockSubscriptionService.Setup(s => s.CancelSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessFailedPaymentsAsync(CancellationToken.None);

        // Assert
        _mockSubscriptionService.Verify(
            s => s.CancelSubscriptionAsync(
                subscription.Id, 
                "Maximum payment retry attempts exceeded", 
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockSubscriptionService.Verify(
            s => s.RetryPaymentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPendingCancellations_WithPendingCancellations_ShouldProcessCancellations()
    {
        // Arrange
        var cancellationDate = DateTime.UtcNow.AddDays(-1);
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.PendingCancellation,
            CancellationDate = cancellationDate,
            Plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Monthly Plan",
                Price = 29.99m,
                BillingCycle = BillingCycle.Monthly
            },
            Customer = new Customer
            {
                Id = Guid.NewGuid(),
                Email = "pending@example.com"
            }
        };

        var subscriptions = new List<Subscription> { subscription }.AsQueryable();
        var mockSubscriptionSet = new Mock<DbSet<Subscription>>();
        
        _mockContext.Setup(c => c.Subscriptions).Returns(mockSubscriptionSet.Object);
        
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Provider).Returns(subscriptions.Provider);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.Expression).Returns(subscriptions.Expression);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.ElementType).Returns(subscriptions.ElementType);
        mockSubscriptionSet.As<IQueryable<Subscription>>()
            .Setup(m => m.GetEnumerator()).Returns(subscriptions.GetEnumerator());

        _mockSubscriptionService.Setup(s => s.FinalizeSubscriptionCancellationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessPendingCancellationsAsync(CancellationToken.None);

        // Assert
        _mockSubscriptionService.Verify(
            s => s.FinalizeSubscriptionCancellationAsync(subscription.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredData_WithOldData_ShouldCleanupData()
    {
        // Arrange
        var cutoffDate = DateTime.UtcNow.AddDays(-_options.CleanupRetentionDays);
        var oldLog = new SubscriptionProcessingLog
        {
            Id = Guid.NewGuid(),
            ProcessedAt = cutoffDate.AddDays(-1),
            SubscriptionId = Guid.NewGuid(),
            ProcessingType = "Renewal",
            Success = true
        };

        var logs = new List<SubscriptionProcessingLog> { oldLog }.AsQueryable();
        var mockLogSet = new Mock<DbSet<SubscriptionProcessingLog>>();
        
        _mockContext.Setup(c => c.SubscriptionProcessingLogs).Returns(mockLogSet.Object);
        
        mockLogSet.As<IQueryable<SubscriptionProcessingLog>>()
            .Setup(m => m.Provider).Returns(logs.Provider);
        mockLogSet.As<IQueryable<SubscriptionProcessingLog>>()
            .Setup(m => m.Expression).Returns(logs.Expression);
        mockLogSet.As<IQueryable<SubscriptionProcessingLog>>()
            .Setup(m => m.ElementType).Returns(logs.ElementType);
        mockLogSet.As<IQueryable<SubscriptionProcessingLog>>()
            .Setup(m => m.GetEnumerator()).Returns(logs.GetEnumerator());

        // Act
        await _service.CleanupExpiredDataAsync(CancellationToken.None);

        // Assert
        mockLogSet.Verify(s => s.RemoveRange(It.IsAny<IEnumerable<SubscriptionProcessingLog>>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CalculateNextRunTime_WithCurrentTime_ShouldReturnCorrectNextTime()
    {
        // Arrange
        var currentTime = new DateTime(2024, 1, 15, 14, 30, 0); // 2:30 PM
        var intervalMinutes = 60;

        // Act
        var nextRunTime = _service.CalculateNextRunTime(currentTime, intervalMinutes);

        // Assert
        var expectedTime = new DateTime(2024, 1, 15, 15, 30, 0); // 3:30 PM
        nextRunTime.Should().Be(expectedTime);
    }

    [Fact]
    public async Task StartAsync_ShouldStartProcessing()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        await _service.StartAsync(cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Subscription Scheduler Service started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldStopProcessing()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        await _service.StopAsync(cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Subscription Scheduler Service stopped")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}