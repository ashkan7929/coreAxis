using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.EventBus;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class EdgeCaseTests
{
    private readonly Mock<IUserReferralRepository> _userReferralRepositoryMock;
    private readonly Mock<ICommissionRuleSetRepository> _ruleSetRepositoryMock;
    private readonly Mock<ICommissionTransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<ILogger<CommissionCalculationService>> _loggerMock;
    private readonly CommissionCalculationService _commissionService;

    public EdgeCaseTests()
    {
        _userReferralRepositoryMock = new Mock<IUserReferralRepository>();
        _ruleSetRepositoryMock = new Mock<ICommissionRuleSetRepository>();
        _transactionRepositoryMock = new Mock<ICommissionTransactionRepository>();
        _eventBusMock = new Mock<IEventBus>();
        _loggerMock = new Mock<ILogger<CommissionCalculationService>>();
        
        _commissionService = new CommissionCalculationService(
            _userReferralRepositoryMock.Object,
            _ruleSetRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _eventBusMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ProcessPayment_WithOrphanedUser_ShouldNotGenerateCommissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        // User exists but has no referral record (orphaned)
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync((UserReferral)null);
        
        _transactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);

        // Act
        await _commissionService.ProcessPaymentConfirmedAsync(
            paymentId, userId, 1000m, productId
        );

        // Assert
        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CommissionTransaction>()),
            Times.Never
        );
        
        _eventBusMock.Verify(
            x => x.PublishAsync(It.IsAny<IDomainEvent>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ProcessPayment_WithCircularReference_ShouldHandleGracefully()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        // Create circular reference scenario (should not happen in real world)
        var referralA = UserReferral.Create(userA, userB, "/" + userB.ToString());
        var referralB = UserReferral.Create(userB, userA, "/" + userA.ToString());
        
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userA))
            .ReturnsAsync(referralA);
        
        _userReferralRepositoryMock
            .Setup(x => x.GetUplineUsersAsync(userA, It.IsAny<int>()))
            .ReturnsAsync(new[] { referralB }); // This creates the circular reference
        
        _transactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);
        
        var ruleSet = CreateDefaultRuleSet();
        _ruleSetRepositoryMock
            .Setup(x => x.GetActiveRuleSetAsync())
            .ReturnsAsync(ruleSet);

        // Act & Assert - Should not throw exception
        await _commissionService.ProcessPaymentConfirmedAsync(
            paymentId, userA, 1000m, productId
        );
        
        // Should still process normally, ignoring the circular reference
        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CommissionTransaction>()),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task ProcessPayment_WithVeryLargeAmount_ShouldHandleCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var parentUserId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var veryLargeAmount = decimal.MaxValue; // Edge case: maximum decimal value
        
        var userReferral = UserReferral.Create(userId, parentUserId, "/" + parentUserId.ToString());
        var parentReferral = UserReferral.CreateRoot(parentUserId);
        
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(userReferral);
        
        _userReferralRepositoryMock
            .Setup(x => x.GetUplineUsersAsync(userId, It.IsAny<int>()))
            .ReturnsAsync(new[] { parentReferral });
        
        _transactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);
        
        var ruleSet = CreateDefaultRuleSet();
        _ruleSetRepositoryMock
            .Setup(x => x.GetActiveRuleSetAsync())
            .ReturnsAsync(ruleSet);

        // Act
        await _commissionService.ProcessPaymentConfirmedAsync(
            paymentId, userId, veryLargeAmount, productId
        );

        // Assert
        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.Is<CommissionTransaction>(ct => 
                ct.Amount > 0 && ct.Amount < veryLargeAmount
            )),
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessPayment_WithZeroAmount_ShouldNotGenerateCommissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var userReferral = UserReferral.CreateRoot(userId);
        
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(userReferral);
        
        _transactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);

        // Act
        await _commissionService.ProcessPaymentConfirmedAsync(
            paymentId, userId, 0m, productId
        );

        // Assert
        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CommissionTransaction>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ProcessPayment_WithNegativeAmount_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _commissionService.ProcessPaymentConfirmedAsync(
                paymentId, userId, -100m, productId
            )
        );
    }

    [Fact]
    public async Task ProcessPayment_WithVeryDeepHierarchy_ShouldRespectMaxLevels()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        // Create a very deep hierarchy (100 levels)
        var uplineUsers = new List<UserReferral>();
        var currentPath = "";
        
        for (int i = 0; i < 100; i++)
        {
            var uplineUserId = Guid.NewGuid();
            currentPath = "/" + uplineUserId.ToString() + currentPath;
            var uplineUser = i == 99 
                ? UserReferral.CreateRoot(uplineUserId)
                : UserReferral.Create(uplineUserId, Guid.NewGuid(), currentPath);
            uplineUsers.Add(uplineUser);
        }
        
        var userReferral = UserReferral.Create(userId, uplineUsers.First().UserId, currentPath);
        
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(userReferral);
        
        _userReferralRepositoryMock
            .Setup(x => x.GetUplineUsersAsync(userId, It.IsAny<int>()))
            .ReturnsAsync(uplineUsers);
        
        _transactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);
        
        var ruleSet = CreateRuleSetWithManyLevels();
        _ruleSetRepositoryMock
            .Setup(x => x.GetActiveRuleSetAsync())
            .ReturnsAsync(ruleSet);

        // Act
        await _commissionService.ProcessPaymentConfirmedAsync(
            paymentId, userId, 1000m, productId
        );

        // Assert - Should only generate commissions for defined levels (not all 100)
        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CommissionTransaction>()),
            Times.Exactly(10) // Only 10 levels defined in rule set
        );
    }

    [Fact]
    public async Task UserReferral_CreateWithInvalidPath_ShouldThrowException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(
            () => UserReferral.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "invalid-path" // Invalid path format
            )
        );
    }

    [Fact]
    public async Task UserReferral_CreateWithSelfReference_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => UserReferral.Create(
                userId,
                userId, // Self reference
                "/" + userId.ToString()
            )
        );
    }

    [Fact]
    public void CommissionRuleSet_CreateVersionWithInvalidJson_ShouldThrowException()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test Rules", "Test Description");
        var invalidJson = "{ invalid json }";
        
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => ruleSet.CreateVersion(invalidJson, "Invalid version")
        );
    }

    [Fact]
    public void CommissionRuleSet_CreateVersionWithEmptyJson_ShouldThrowException()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test Rules", "Test Description");
        
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => ruleSet.CreateVersion("", "Empty version")
        );
        
        Assert.Throws<ArgumentException>(
            () => ruleSet.CreateVersion(null, "Null version")
        );
    }

    [Fact]
    public void CommissionTransaction_CreateWithInvalidAmount_ShouldThrowException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(
            () => CommissionTransaction.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                -100m, // Negative amount
                1,
                Guid.NewGuid(),
                Guid.NewGuid()
            )
        );
        
        Assert.Throws<ArgumentException>(
            () => CommissionTransaction.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                0m, // Zero amount
                1,
                Guid.NewGuid(),
                Guid.NewGuid()
            )
        );
    }

    [Fact]
    public void CommissionTransaction_CreateWithInvalidLevel_ShouldThrowException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(
            () => CommissionTransaction.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                100m,
                0, // Invalid level
                Guid.NewGuid(),
                Guid.NewGuid()
            )
        );
        
        Assert.Throws<ArgumentException>(
            () => CommissionTransaction.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                100m,
                -1, // Negative level
                Guid.NewGuid(),
                Guid.NewGuid()
            )
        );
    }

    [Fact]
    public async Task ProcessPayment_WithConcurrentSamePayment_ShouldHandleIdempotency()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var userReferral = UserReferral.CreateRoot(userId);
        
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(userReferral);
        
        // First call returns false, second returns true (simulating concurrent processing)
        _transactionRepositoryMock
            .SetupSequence(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        // Act
        var task1 = _commissionService.ProcessPaymentConfirmedAsync(
            paymentId, userId, 1000m, productId
        );
        
        var task2 = _commissionService.ProcessPaymentConfirmedAsync(
            paymentId, userId, 1000m, productId
        );
        
        await Task.WhenAll(task1, task2);

        // Assert - Should only process once due to idempotency
        _transactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CommissionTransaction>()),
            Times.Never // Second call should be skipped
        );
    }

    private CommissionRuleSet CreateDefaultRuleSet()
    {
        var ruleSet = CommissionRuleSet.Create("Test Rules", "Test Description");
        var schemaJson = """
        {
            "levels": [
                { "level": 1, "percentage": 10 }
            ]
        }
        """;
        var version = ruleSet.CreateVersion(schemaJson, "Test version");
        version.Publish();
        ruleSet.Activate();
        return ruleSet;
    }

    private CommissionRuleSet CreateRuleSetWithManyLevels()
    {
        var ruleSet = CommissionRuleSet.Create("Many Levels Rules", "Test Description");
        var levels = string.Join(",\n", 
            Enumerable.Range(1, 10).Select(i => $"{{ \"level\": {i}, \"percentage\": {11 - i} }}"));
        
        var schemaJson = $@"
        {{
            ""levels"": [
                {levels}
            ]
        }}
        ";
        
        var version = ruleSet.CreateVersion(schemaJson, "Many levels version");
        version.Publish();
        ruleSet.Activate();
        return ruleSet;
    }
}