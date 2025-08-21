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

public class ReservationServiceTests : IDisposable
{
    private readonly Mock<ICommerceDbContext> _mockContext;
    private readonly Mock<IDomainEventDispatcher> _mockEventDispatcher;
    private readonly Mock<ILogger<ReservationService>> _mockLogger;
    private readonly ReservationService _service;
    private readonly DbContextOptions<TestDbContext> _dbOptions;
    private readonly TestDbContext _dbContext;

    public ReservationServiceTests()
    {
        _mockContext = new Mock<ICommerceDbContext>();
        _mockEventDispatcher = new Mock<IDomainEventDispatcher>();
        _mockLogger = new Mock<ILogger<ReservationService>>();

        // Setup in-memory database for integration tests
        _dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TestDbContext(_dbOptions);

        _service = new ReservationService(_mockContext.Object, _mockEventDispatcher.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateReservationsAsync_WithSufficientInventory_ShouldCreateReservations()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            VariantId = variantId,
            AvailableQuantity = 100,
            ReservedQuantity = 0,
            CommittedQuantity = 0,
            Version = 1
        };

        var requests = new List<ReservationRequest>
        {
            new ReservationRequest
            {
                ProductId = productId,
                VariantId = variantId,
                Quantity = 5,
                CustomerId = customerId,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        var mockInventorySet = new Mock<DbSet<InventoryItem>>();
        var mockReservationSet = new Mock<DbSet<InventoryReservation>>();
        
        _mockContext.Setup(c => c.InventoryItems).Returns(mockInventorySet.Object);
        _mockContext.Setup(c => c.InventoryReservations).Returns(mockReservationSet.Object);
        
        mockInventorySet.Setup(s => s.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync(inventoryItem);

        // Act
        var result = await _service.CreateReservationsAsync(requests);

        // Assert
        result.Success.Should().BeTrue();
        result.Reservations.Should().HaveCount(1);
        result.Reservations.First().Success.Should().BeTrue();
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<InventoryReservationCreatedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateReservationsAsync_WithInsufficientInventory_ShouldFail()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        
        var inventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            VariantId = variantId,
            AvailableQuantity = 2,
            ReservedQuantity = 0,
            CommittedQuantity = 0,
            Version = 1
        };

        var requests = new List<ReservationRequest>
        {
            new ReservationRequest
            {
                ProductId = productId,
                VariantId = variantId,
                Quantity = 5,
                CustomerId = customerId,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        var mockInventorySet = new Mock<DbSet<InventoryItem>>();
        
        _mockContext.Setup(c => c.InventoryItems).Returns(mockInventorySet.Object);
        
        mockInventorySet.Setup(s => s.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync(inventoryItem);

        // Act
        var result = await _service.CreateReservationsAsync(requests);

        // Assert
        result.Success.Should().BeFalse();
        result.Reservations.Should().HaveCount(1);
        result.Reservations.First().Success.Should().BeFalse();
        result.Reservations.First().FailureReason.Should().Contain("Insufficient inventory");
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<InventoryReservationFailedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmReservationsAsync_WithValidReservations_ShouldConfirm()
    {
        // Arrange
        var reservationIds = new List<Guid> { Guid.NewGuid() };
        var customerId = Guid.NewGuid();
        
        var reservation = new InventoryReservation
        {
            Id = reservationIds.First(),
            CustomerId = customerId,
            Quantity = 5,
            Status = ReservationStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var mockReservationSet = new Mock<DbSet<InventoryReservation>>();
        
        _mockContext.Setup(c => c.InventoryReservations).Returns(mockReservationSet.Object);
        
        mockReservationSet.Setup(s => s.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _service.ConfirmReservationsAsync(reservationIds, customerId);

        // Assert
        result.Success.Should().BeTrue();
        result.ConfirmedReservations.Should().HaveCount(1);
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<InventoryReservationConfirmedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReleaseReservationsAsync_WithValidReservations_ShouldRelease()
    {
        // Arrange
        var reservationIds = new List<Guid> { Guid.NewGuid() };
        var customerId = Guid.NewGuid();
        
        var reservation = new InventoryReservation
        {
            Id = reservationIds.First(),
            CustomerId = customerId,
            Quantity = 5,
            Status = ReservationStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var mockReservationSet = new Mock<DbSet<InventoryReservation>>();
        
        _mockContext.Setup(c => c.InventoryReservations).Returns(mockReservationSet.Object);
        
        mockReservationSet.Setup(s => s.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _service.ReleaseReservationsAsync(reservationIds, customerId);

        // Assert
        result.Success.Should().BeTrue();
        result.ReleasedReservations.Should().HaveCount(1);
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<InventoryReservationReleasedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredReservationsAsync_WithExpiredReservations_ShouldCleanup()
    {
        // Arrange
        var expiredReservation = new InventoryReservation
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Quantity = 5,
            Status = ReservationStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddHours(-1) // Expired
        };

        var mockReservationSet = new Mock<DbSet<InventoryReservation>>();
        var reservations = new List<InventoryReservation> { expiredReservation }.AsQueryable();
        
        _mockContext.Setup(c => c.InventoryReservations).Returns(mockReservationSet.Object);
        
        mockReservationSet.As<IQueryable<InventoryReservation>>()
            .Setup(m => m.Provider).Returns(reservations.Provider);
        mockReservationSet.As<IQueryable<InventoryReservation>>()
            .Setup(m => m.Expression).Returns(reservations.Expression);
        mockReservationSet.As<IQueryable<InventoryReservation>>()
            .Setup(m => m.ElementType).Returns(reservations.ElementType);
        mockReservationSet.As<IQueryable<InventoryReservation>>()
            .Setup(m => m.GetEnumerator()).Returns(reservations.GetEnumerator());

        // Act
        var result = await _service.CleanupExpiredReservationsAsync();

        // Assert
        result.Should().Be(1);
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<InventoryReservationExpiredEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateReservationsAsync_WithInvalidQuantity_ShouldFail(int quantity)
    {
        // Arrange
        var requests = new List<ReservationRequest>
        {
            new ReservationRequest
            {
                ProductId = Guid.NewGuid(),
                VariantId = Guid.NewGuid(),
                Quantity = quantity,
                CustomerId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }
        };

        // Act
        var result = await _service.CreateReservationsAsync(requests);

        // Assert
        result.Success.Should().BeFalse();
        result.Reservations.First().FailureReason.Should().Contain("Invalid quantity");
    }

    [Fact]
    public async Task CreateReservationsAsync_WithPastExpirationDate_ShouldFail()
    {
        // Arrange
        var requests = new List<ReservationRequest>
        {
            new ReservationRequest
            {
                ProductId = Guid.NewGuid(),
                VariantId = Guid.NewGuid(),
                Quantity = 5,
                CustomerId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddHours(-1) // Past date
            }
        };

        // Act
        var result = await _service.CreateReservationsAsync(requests);

        // Assert
        result.Success.Should().BeFalse();
        result.Reservations.First().FailureReason.Should().Contain("Expiration date must be in the future");
    }

    [Fact]
    public async Task ConfirmReservationsAsync_WithWrongCustomer_ShouldFail()
    {
        // Arrange
        var reservationIds = new List<Guid> { Guid.NewGuid() };
        var customerId = Guid.NewGuid();
        var wrongCustomerId = Guid.NewGuid();
        
        var reservation = new InventoryReservation
        {
            Id = reservationIds.First(),
            CustomerId = customerId, // Different customer
            Quantity = 5,
            Status = ReservationStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var mockReservationSet = new Mock<DbSet<InventoryReservation>>();
        
        _mockContext.Setup(c => c.InventoryReservations).Returns(mockReservationSet.Object);
        
        mockReservationSet.Setup(s => s.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _service.ConfirmReservationsAsync(reservationIds, wrongCustomerId);

        // Assert
        result.Success.Should().BeFalse();
        result.FailedReservations.Should().HaveCount(1);
        result.FailedReservations.First().Reason.Should().Contain("not owned by customer");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}

// Test DbContext for integration tests
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    
    public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
    public DbSet<InventoryReservation> InventoryReservations { get; set; } = null!;
    public DbSet<InventoryLedger> InventoryLedgers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<ReconciliationSession> ReconciliationSessions { get; set; } = null!;
    public DbSet<ReconciliationEntry> ReconciliationEntries { get; set; } = null!;
}