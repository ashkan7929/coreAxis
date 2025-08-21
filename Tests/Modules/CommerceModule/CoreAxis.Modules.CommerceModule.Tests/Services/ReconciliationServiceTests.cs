using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Events;
using CoreAxis.Shared.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace CoreAxis.Modules.CommerceModule.Tests.Services;

public class ReconciliationServiceTests
{
    private readonly Mock<ICommerceDbContext> _mockContext;
    private readonly Mock<IDomainEventDispatcher> _mockEventDispatcher;
    private readonly Mock<ILogger<ReconciliationService>> _mockLogger;
    private readonly ReconciliationService _service;

    public ReconciliationServiceTests()
    {
        _mockContext = new Mock<ICommerceDbContext>();
        _mockEventDispatcher = new Mock<IDomainEventDispatcher>();
        _mockLogger = new Mock<ILogger<ReconciliationService>>();
        _service = new ReconciliationService(_mockContext.Object, _mockEventDispatcher.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessStatementAsync_WithMatchingTransactions_ShouldReconcileSuccessfully()
    {
        // Arrange
        var paymentProvider = "Stripe";
        var transactionId = "txn_123";
        var externalTransactionId = "pi_456";
        var amount = 100.00m;
        var currency = "USD";
        var transactionDate = DateTime.UtcNow;
        
        var statement = new PaymentGatewayStatement
        {
            Id = "stmt_001",
            PaymentProvider = paymentProvider,
            PeriodStart = DateTime.UtcNow.AddDays(-1),
            PeriodEnd = DateTime.UtcNow,
            Transactions = new List<StatementTransaction>
            {
                new StatementTransaction
                {
                    Id = transactionId,
                    ExternalTransactionId = externalTransactionId,
                    Amount = amount,
                    Currency = currency,
                    TransactionDate = transactionDate,
                    ReferenceNumber = "REF123",
                    TransactionType = "Payment",
                    Status = "Completed"
                }
            }
        };

        var matchingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            ExternalTransactionId = externalTransactionId,
            Amount = amount,
            Currency = currency,
            PaymentProvider = paymentProvider,
            Status = PaymentStatus.Completed,
            CreatedAt = transactionDate,
            Order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-001"
            }
        };

        var mockSessionSet = new Mock<DbSet<ReconciliationSession>>();
        var mockEntrySet = new Mock<DbSet<ReconciliationEntry>>();
        var mockPaymentSet = new Mock<DbSet<Payment>>();
        
        var payments = new List<Payment> { matchingPayment }.AsQueryable();
        
        _mockContext.Setup(c => c.ReconciliationSessions).Returns(mockSessionSet.Object);
        _mockContext.Setup(c => c.ReconciliationEntries).Returns(mockEntrySet.Object);
        _mockContext.Setup(c => c.Payments).Returns(mockPaymentSet.Object);
        
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.Provider).Returns(payments.Provider);
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.Expression).Returns(payments.Expression);
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.ElementType).Returns(payments.ElementType);
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.GetEnumerator()).Returns(payments.GetEnumerator());

        // Act
        var result = await _service.ProcessStatementAsync(statement);

        // Assert
        result.Success.Should().BeTrue();
        result.Summary.Should().NotBeNull();
        result.Summary.TotalTransactions.Should().Be(1);
        result.Summary.MatchedTransactions.Should().Be(1);
        result.Summary.UnmatchedTransactions.Should().Be(0);
        result.Summary.MatchRate.Should().Be(100);
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<ReconciliationCompletedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<TransactionMatchedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessStatementAsync_WithUnmatchedTransactions_ShouldRecordUnmatched()
    {
        // Arrange
        var paymentProvider = "PayPal";
        var transactionId = "txn_456";
        var amount = 150.00m;
        var currency = "USD";
        var transactionDate = DateTime.UtcNow;
        
        var statement = new PaymentGatewayStatement
        {
            Id = "stmt_002",
            PaymentProvider = paymentProvider,
            PeriodStart = DateTime.UtcNow.AddDays(-1),
            PeriodEnd = DateTime.UtcNow,
            Transactions = new List<StatementTransaction>
            {
                new StatementTransaction
                {
                    Id = transactionId,
                    ExternalTransactionId = "pp_789",
                    Amount = amount,
                    Currency = currency,
                    TransactionDate = transactionDate,
                    ReferenceNumber = "REF456",
                    TransactionType = "Payment",
                    Status = "Completed"
                }
            }
        };

        var mockSessionSet = new Mock<DbSet<ReconciliationSession>>();
        var mockEntrySet = new Mock<DbSet<ReconciliationEntry>>();
        var mockPaymentSet = new Mock<DbSet<Payment>>();
        
        var payments = new List<Payment>().AsQueryable(); // No matching payments
        
        _mockContext.Setup(c => c.ReconciliationSessions).Returns(mockSessionSet.Object);
        _mockContext.Setup(c => c.ReconciliationEntries).Returns(mockEntrySet.Object);
        _mockContext.Setup(c => c.Payments).Returns(mockPaymentSet.Object);
        
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.Provider).Returns(payments.Provider);
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.Expression).Returns(payments.Expression);
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.ElementType).Returns(payments.ElementType);
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.GetEnumerator()).Returns(payments.GetEnumerator());

        // Act
        var result = await _service.ProcessStatementAsync(statement);

        // Assert
        result.Success.Should().BeTrue();
        result.Summary.Should().NotBeNull();
        result.Summary.TotalTransactions.Should().Be(1);
        result.Summary.MatchedTransactions.Should().Be(0);
        result.Summary.UnmatchedTransactions.Should().Be(1);
        result.Summary.MatchRate.Should().Be(0);
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<TransactionUnmatchedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessStatementAsync_WithAmountDiscrepancy_ShouldDetectDiscrepancy()
    {
        // Arrange
        var paymentProvider = "Stripe";
        var transactionId = "txn_789";
        var externalTransactionId = "pi_101";
        var statementAmount = 100.00m;
        var systemAmount = 99.50m; // Small discrepancy
        var currency = "USD";
        var transactionDate = DateTime.UtcNow;
        
        var statement = new PaymentGatewayStatement
        {
            Id = "stmt_003",
            PaymentProvider = paymentProvider,
            PeriodStart = DateTime.UtcNow.AddDays(-1),
            PeriodEnd = DateTime.UtcNow,
            Transactions = new List<StatementTransaction>
            {
                new StatementTransaction
                {
                    Id = transactionId,
                    ExternalTransactionId = externalTransactionId,
                    Amount = statementAmount,
                    Currency = currency,
                    TransactionDate = transactionDate,
                    ReferenceNumber = "REF789",
                    TransactionType = "Payment",
                    Status = "Completed"
                }
            }
        };

        var matchingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            ExternalTransactionId = externalTransactionId,
            Amount = systemAmount,
            Currency = currency,
            PaymentProvider = paymentProvider,
            Status = PaymentStatus.Completed,
            CreatedAt = transactionDate,
            Order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-002"
            }
        };

        var mockSessionSet = new Mock<DbSet<ReconciliationSession>>();
        var mockEntrySet = new Mock<DbSet<ReconciliationEntry>>();
        var mockPaymentSet = new Mock<DbSet<Payment>>();
        
        var payments = new List<Payment> { matchingPayment }.AsQueryable();
        
        _mockContext.Setup(c => c.ReconciliationSessions).Returns(mockSessionSet.Object);
        _mockContext.Setup(c => c.ReconciliationEntries).Returns(mockEntrySet.Object);
        _mockContext.Setup(c => c.Payments).Returns(mockPaymentSet.Object);
        
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.Provider).Returns(payments.Provider);
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.Expression).Returns(payments.Expression);
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.ElementType).Returns(payments.ElementType);
        mockPaymentSet.As<IQueryable<Payment>>()
            .Setup(m => m.GetEnumerator()).Returns(payments.GetEnumerator());

        // Act
        var result = await _service.ProcessStatementAsync(statement);

        // Assert
        result.Success.Should().BeTrue();
        result.Summary.MatchedTransactions.Should().Be(1);
        result.Report.Should().NotBeNull();
        result.Report.AmountDiscrepancies.Should().HaveCount(1);
        result.Report.AmountDiscrepancies.First().Difference.Should().Be(0.50m);
    }

    [Theory]
    [InlineData(100.00, 100.00, 0, 100)] // Exact match
    [InlineData(100.00, 99.99, 1, 95)]  // Small amount difference
    [InlineData(100.00, 95.00, 24, 70)] // Larger amount difference, within time window
    [InlineData(100.00, 100.00, 25, 90)] // Exact amount, slightly outside ideal time window
    public void CalculateMatchConfidence_WithDifferentScenarios_ShouldCalculateCorrectly(
        decimal statementAmount, decimal paymentAmount, int hoursDifference, int expectedMinConfidence)
    {
        // Arrange
        var transactionDate = DateTime.UtcNow;
        var paymentDate = transactionDate.AddHours(-hoursDifference);
        
        var transaction = new StatementTransaction
        {
            Amount = statementAmount,
            Currency = "USD",
            TransactionDate = transactionDate,
            ReferenceNumber = "REF123"
        };

        var payment = new Payment
        {
            Amount = paymentAmount,
            Currency = "USD",
            CreatedAt = paymentDate,
            ReferenceNumber = "REF123",
            Order = new Order { OrderNumber = "ORD-001" }
        };

        // Act
        var confidence = _service.CalculateMatchConfidence(transaction, payment);

        // Assert
        confidence.Should().BeGreaterOrEqualTo(expectedMinConfidence);
    }

    [Fact]
    public async Task GetReconciliationHistoryAsync_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var paymentProvider = "Stripe";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        
        var sessions = new List<ReconciliationSession>
        {
            new ReconciliationSession
            {
                Id = Guid.NewGuid(),
                PaymentProvider = paymentProvider,
                PeriodStart = startDate.AddDays(1),
                PeriodEnd = startDate.AddDays(2),
                Status = ReconciliationStatus.Completed
            },
            new ReconciliationSession
            {
                Id = Guid.NewGuid(),
                PaymentProvider = "PayPal", // Different provider
                PeriodStart = startDate.AddDays(1),
                PeriodEnd = startDate.AddDays(2),
                Status = ReconciliationStatus.Completed
            }
        }.AsQueryable();

        var mockSessionSet = new Mock<DbSet<ReconciliationSession>>();
        
        _mockContext.Setup(c => c.ReconciliationSessions).Returns(mockSessionSet.Object);
        
        mockSessionSet.As<IQueryable<ReconciliationSession>>()
            .Setup(m => m.Provider).Returns(sessions.Provider);
        mockSessionSet.As<IQueryable<ReconciliationSession>>()
            .Setup(m => m.Expression).Returns(sessions.Expression);
        mockSessionSet.As<IQueryable<ReconciliationSession>>()
            .Setup(m => m.ElementType).Returns(sessions.ElementType);
        mockSessionSet.As<IQueryable<ReconciliationSession>>()
            .Setup(m => m.GetEnumerator()).Returns(sessions.GetEnumerator());

        // Act
        var result = await _service.GetReconciliationHistoryAsync(paymentProvider, startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        result.First().PaymentProvider.Should().Be(paymentProvider);
    }

    [Fact]
    public async Task GetSessionDetailsAsync_WithValidSessionId_ShouldReturnDetails()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        
        var session = new ReconciliationSession
        {
            Id = sessionId,
            PaymentProvider = "Stripe",
            Status = ReconciliationStatus.Completed
        };

        var entries = new List<ReconciliationEntry>
        {
            new ReconciliationEntry
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                MatchStatus = MatchStatus.Matched,
                MatchConfidence = 95
            }
        }.AsQueryable();

        var mockSessionSet = new Mock<DbSet<ReconciliationSession>>();
        var mockEntrySet = new Mock<DbSet<ReconciliationEntry>>();
        
        _mockContext.Setup(c => c.ReconciliationSessions).Returns(mockSessionSet.Object);
        _mockContext.Setup(c => c.ReconciliationEntries).Returns(mockEntrySet.Object);
        
        mockSessionSet.Setup(s => s.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ReconciliationSession, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        
        mockEntrySet.As<IQueryable<ReconciliationEntry>>()
            .Setup(m => m.Provider).Returns(entries.Provider);
        mockEntrySet.As<IQueryable<ReconciliationEntry>>()
            .Setup(m => m.Expression).Returns(entries.Expression);
        mockEntrySet.As<IQueryable<ReconciliationEntry>>()
            .Setup(m => m.ElementType).Returns(entries.ElementType);
        mockEntrySet.As<IQueryable<ReconciliationEntry>>()
            .Setup(m => m.GetEnumerator()).Returns(entries.GetEnumerator());

        // Act
        var result = await _service.GetSessionDetailsAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result.Session.Should().Be(session);
        result.Entries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSessionDetailsAsync_WithInvalidSessionId_ShouldThrowException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        
        var mockSessionSet = new Mock<DbSet<ReconciliationSession>>();
        
        _mockContext.Setup(c => c.ReconciliationSessions).Returns(mockSessionSet.Object);
        
        mockSessionSet.Setup(s => s.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ReconciliationSession, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationSession?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetSessionDetailsAsync(sessionId));
    }

    [Fact]
    public void DetermineUnmatchedReason_WithNoCandidates_ShouldReturnNoMatchesFound()
    {
        // Arrange
        var transaction = new StatementTransaction
        {
            Id = "txn_001",
            Amount = 100.00m
        };
        var candidates = new List<PaymentMatchCandidate>();

        // Act
        var reason = _service.DetermineUnmatchedReason(transaction, candidates);

        // Assert
        reason.Should().Be("No potential matches found in the system");
    }

    [Fact]
    public void DetermineUnmatchedReason_WithLargeAmountDifference_ShouldReturnAmountMismatch()
    {
        // Arrange
        var transaction = new StatementTransaction
        {
            Id = "txn_001",
            Amount = 100.00m
        };
        
        var candidates = new List<PaymentMatchCandidate>
        {
            new PaymentMatchCandidate
            {
                Payment = new Payment { Amount = 150.00m },
                Confidence = 60,
                AmountDifference = 50.00m,
                TimeDifference = 30
            }
        };

        // Act
        var reason = _service.DetermineUnmatchedReason(transaction, candidates);

        // Assert
        reason.Should().Contain("Amount mismatch");
        reason.Should().Contain("$100.00");
        reason.Should().Contain("$150.00");
    }

    [Fact]
    public void DetermineUnmatchedReason_WithLargeTimeDifference_ShouldReturnTimeDifference()
    {
        // Arrange
        var transaction = new StatementTransaction
        {
            Id = "txn_001",
            Amount = 100.00m
        };
        
        var candidates = new List<PaymentMatchCandidate>
        {
            new PaymentMatchCandidate
            {
                Payment = new Payment { Amount = 100.00m },
                Confidence = 60,
                AmountDifference = 0.00m,
                TimeDifference = 5000 // > 72 hours
            }
        };

        // Act
        var reason = _service.DetermineUnmatchedReason(transaction, candidates);

        // Assert
        reason.Should().Contain("Time difference too large");
    }
}