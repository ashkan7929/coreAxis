using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.Modules.MLMModule.Domain.ValueObjects;
using CoreAxis.Modules.MLMModule.Infrastructure.Outbox;
using CoreAxis.SharedKernel.Domain.Events;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class CommissionCalculationServiceTests
{
    private readonly Mock<IUserReferralRepository> _userReferralRepositoryMock;
    private readonly Mock<ICommissionRuleSetRepository> _commissionRuleSetRepositoryMock;
    private readonly Mock<ICommissionTransactionRepository> _commissionTransactionRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<ILogger<CommissionCalculationService>> _loggerMock;
    private readonly CommissionCalculationService _service;

    public CommissionCalculationServiceTests()
    {
        _userReferralRepositoryMock = new Mock<IUserReferralRepository>();
        _commissionRuleSetRepositoryMock = new Mock<ICommissionRuleSetRepository>();
        _commissionTransactionRepositoryMock = new Mock<ICommissionTransactionRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _loggerMock = new Mock<ILogger<CommissionCalculationService>>();
        
        _service = new CommissionCalculationService(
            _userReferralRepositoryMock.Object,
            _commissionRuleSetRepositoryMock.Object,
            _commissionTransactionRepositoryMock.Object,
            _outboxServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ProcessPaymentConfirmed_WithValidPayment_ShouldGenerateCommissions()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = 1000m;
        var productId = Guid.NewGuid();
        
        var userReferral = UserReferral.Create(
            userId,
            null, // No parent
            "1", // Root path
            ReferralStatus.Active,
            DateTime.UtcNow
        );
        
        var ruleSet = CommissionRuleSet.Create(
            "Test RuleSet",
            "Test product commission rules",
            true
        );
        
        var ruleVersion = ruleSet.CreateVersion(
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            userId
        );
        
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(userReferral);
            
        _commissionRuleSetRepositoryMock
            .Setup(x => x.GetActiveRuleSetForProductAsync(productId))
            .ReturnsAsync(ruleSet);
            
        _commissionTransactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);

        // Act
        await _service.ProcessPaymentConfirmedAsync(paymentId, userId, amount, productId);

        // Assert
        _commissionTransactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CommissionTransaction>()), 
            Times.AtLeastOnce
        );
        
        _outboxServiceMock.Verify(
            x => x.AddAsync(It.IsAny<IDomainEvent>()), 
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task ProcessPaymentConfirmed_WithDuplicatePayment_ShouldNotGenerateCommissions()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = 1000m;
        var productId = Guid.NewGuid();
        
        _commissionTransactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(true);

        // Act
        await _service.ProcessPaymentConfirmedAsync(paymentId, userId, amount, productId);

        // Assert
        _commissionTransactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CommissionTransaction>()), 
            Times.Never
        );
        
        _outboxServiceMock.Verify(
            x => x.AddAsync(It.IsAny<IDomainEvent>()), 
            Times.Never
        );
    }

    [Fact]
    public async Task ProcessPaymentConfirmed_WithInactiveUser_ShouldNotGenerateCommissions()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = 1000m;
        var productId = Guid.NewGuid();
        
        var userReferral = UserReferral.Create(
            userId,
            null,
            "1",
            ReferralStatus.Inactive,
            DateTime.UtcNow
        );
        
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(userReferral);
            
        _commissionTransactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);

        // Act
        await _service.ProcessPaymentConfirmedAsync(paymentId, userId, amount, productId);

        // Assert
        _commissionTransactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CommissionTransaction>()), 
            Times.Never
        );
    }

    [Fact]
    public async Task CalculateCommissionsForUpline_WithMultipleLevels_ShouldCalculateCorrectAmounts()
    {
        // Arrange
        var level1UserId = Guid.NewGuid();
        var level2UserId = Guid.NewGuid();
        var level3UserId = Guid.NewGuid();
        
        var level3User = UserReferral.Create(
            level3UserId,
            level2UserId,
            "1.2.3",
            ReferralStatus.Active,
            DateTime.UtcNow
        );
        
        var level2User = UserReferral.Create(
            level2UserId,
            level1UserId,
            "1.2",
            ReferralStatus.Active,
            DateTime.UtcNow
        );
        
        var level1User = UserReferral.Create(
            level1UserId,
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow
        );
        
        var uplineUsers = new List<UserReferral> { level2User, level1User };
        
        _userReferralRepositoryMock
            .Setup(x => x.GetUplineUsersAsync(level3UserId))
            .ReturnsAsync(uplineUsers);
        
        var ruleSet = CommissionRuleSet.Create(
            "Multi-level RuleSet",
            "Multi-level commission rules",
            true
        );
        
        var ruleVersion = ruleSet.CreateVersion(
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}, {\"level\": 2, \"percentage\": 5}]}",
            level1UserId
        );
        
        var amount = 1000m;
        var productId = Guid.NewGuid();
        
        _commissionRuleSetRepositoryMock
            .Setup(x => x.GetActiveRuleSetForProductAsync(productId))
            .ReturnsAsync(ruleSet);

        // Act
        var commissions = await _service.CalculateCommissionsForUplineAsync(
            level3UserId, 
            amount, 
            productId
        );

        // Assert
        Assert.Equal(2, commissions.Count);
        Assert.Equal(100m, commissions.First(c => c.UserId == level2UserId).Amount); // 10% of 1000
        Assert.Equal(50m, commissions.First(c => c.UserId == level1UserId).Amount);  // 5% of 1000
    }
}