using CoreAxis.Modules.MLMModule.Application.Contracts;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Events;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.SharedKernel.Application.Outbox;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using CoreAxis.SharedKernel.Infrastructure.Outbox;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class MLMUnitTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserReferralRepository> _userReferralRepositoryMock;
    private readonly Mock<ICommissionRuleSetRepository> _commissionRuleSetRepositoryMock;
    private readonly Mock<ICommissionTransactionRepository> _commissionTransactionRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<ILogger<CommissionCalculationService>> _loggerMock;
    private readonly CommissionCalculationService _commissionService;

    public MLMUnitTests(ITestOutputHelper output)
    {
        _output = output;
        _userReferralRepositoryMock = new Mock<IUserReferralRepository>();
        _commissionRuleSetRepositoryMock = new Mock<ICommissionRuleSetRepository>();
        _commissionTransactionRepositoryMock = new Mock<ICommissionTransactionRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _loggerMock = new Mock<ILogger<CommissionCalculationService>>();

        _commissionService = new CommissionCalculationService(
            _userReferralRepositoryMock.Object,
            _commissionRuleSetRepositoryMock.Object,
            _commissionTransactionRepositoryMock.Object,
            _outboxServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ProcessPaymentConfirmed_WithValidMultiLevelNetwork_ShouldGenerateCorrectCommissions()
    {
        // Arrange
        var rootUserId = Guid.NewGuid();
        var level2UserId = Guid.NewGuid();
        var level3UserId = Guid.NewGuid();
        var buyerUserId = Guid.NewGuid();

        var rootUser = UserReferral.CreateRoot(rootUserId);
        var level2User = UserReferral.Create(level2UserId, rootUser);
        var level3User = UserReferral.Create(level3UserId, level2User);
        var buyerUser = UserReferral.Create(buyerUserId, level3User);

        // Setup commission rules
        var ruleSet = CommissionRuleSet.Create(
            "Standard MLM Rules",
            "Standard commission structure"
        );

        var ruleVersion = ruleSet.CreateVersion(
            """
            {
                "levels": [
                    { "level": 1, "percentage": 10 },
                    { "level": 2, "percentage": 5 },
                    { "level": 3, "percentage": 2 }
                ]
            }
            """,
            "Standard rules"
        );
        ruleVersion.Publish();
        ruleSet.Activate();

        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var purchaseAmount = 1000m;

        // Setup repository mocks
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(buyerUserId))
            .ReturnsAsync(buyerUser);
            
        _userReferralRepositoryMock
            .Setup(x => x.GetUplineUsersAsync(buyerUserId))
            .ReturnsAsync(new List<UserReferral> { level3User, level2User, rootUser });
            
        _commissionRuleSetRepositoryMock
            .Setup(x => x.GetActiveRuleSetAsync())
            .ReturnsAsync(ruleSet);
            
        _commissionTransactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);

        var capturedTransactions = new List<CommissionTransaction>();
        _commissionTransactionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<CommissionTransaction>()))
            .Callback<CommissionTransaction>(t => capturedTransactions.Add(t));

        var capturedEvents = new List<IDomainEvent>();
        _outboxServiceMock
            .Setup(x => x.AddAsync(It.IsAny<IDomainEvent>()))
            .Callback<IDomainEvent>(e => capturedEvents.Add(e));

        // Act
        await _commissionService.ProcessPaymentConfirmedAsync(
            paymentId, 
            buyerUserId, 
            purchaseAmount, 
            productId
        );

        // Assert
        Assert.Equal(3, capturedTransactions.Count);
        
        // Verify Level 1 commission (direct parent - level3User)
        var level1Commission = capturedTransactions.First(t => t.UserId == level3UserId);
        Assert.Equal(100m, level1Commission.Amount); // 10% of 1000
        Assert.Equal(1, level1Commission.Level);
        Assert.Equal(CommissionStatus.Pending, level1Commission.Status);
        
        // Verify Level 2 commission (level2User)
        var level2Commission = capturedTransactions.First(t => t.UserId == level2UserId);
        Assert.Equal(50m, level2Commission.Amount); // 5% of 1000
        Assert.Equal(2, level2Commission.Level);
        Assert.Equal(CommissionStatus.Pending, level2Commission.Status);
        
        // Verify Level 3 commission (rootUser)
        var level3Commission = capturedTransactions.First(t => t.UserId == rootUserId);
        Assert.Equal(20m, level3Commission.Amount); // 2% of 1000
        Assert.Equal(3, level3Commission.Level);
        Assert.Equal(CommissionStatus.Pending, level3Commission.Status);
        
        // Verify events
        Assert.Equal(3, capturedEvents.Count);
        Assert.All(capturedEvents, e => Assert.IsType<CommissionGeneratedEvent>(e));
        
        // Verify repository calls
        _commissionTransactionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CommissionTransaction>()), 
            Times.Exactly(3)
        );
        
        _outboxServiceMock.Verify(
            x => x.AddAsync(It.IsAny<IDomainEvent>()), 
            Times.Exactly(3)
        );
    }

    [Fact]
    public async Task ProcessPaymentConfirmed_WithInactiveUserInChain_ShouldSkipInactiveUser()
    {
        // Arrange
        var rootUserId = Guid.NewGuid();
        var level2UserId = Guid.NewGuid();
        var buyerUserId = Guid.NewGuid();

        var rootUser = UserReferral.CreateRoot(rootUserId);
        var level2User = UserReferral.Create(level2UserId, rootUser);
        level2User.Deactivate(); // Make user inactive
        var buyerUser = UserReferral.Create(buyerUserId, level2User);

        var ruleSet = CommissionRuleSet.Create(
            "Standard MLM Rules",
            "Standard commission structure"
        );

        var ruleVersion = ruleSet.CreateVersion(
            """
            {
                "levels": [
                    { "level": 1, "percentage": 10 },
                    { "level": 2, "percentage": 5 }
                ]
            }
            """,
            "Standard rules"
        );
        ruleVersion.Publish();
        ruleSet.Activate();

        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var purchaseAmount = 1000m;

        // Setup mocks
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(buyerUserId))
            .ReturnsAsync(buyerUser);
            
        _userReferralRepositoryMock
            .Setup(x => x.GetUplineUsersAsync(buyerUserId))
            .ReturnsAsync(new List<UserReferral> { level2User, rootUser });
            
        _commissionRuleSetRepositoryMock
            .Setup(x => x.GetActiveRuleSetAsync())
            .ReturnsAsync(ruleSet);
            
        _commissionTransactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);

        var capturedTransactions = new List<CommissionTransaction>();
        _commissionTransactionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<CommissionTransaction>()))
            .Callback<CommissionTransaction>(t => capturedTransactions.Add(t));

        // Act
        await _commissionService.ProcessPaymentConfirmedAsync(
            paymentId, 
            buyerUserId, 
            purchaseAmount, 
            productId
        );

        // Assert
        // Should only generate commission for root user (level 2), skipping inactive level2User
        Assert.Single(capturedTransactions);
        
        var commission = capturedTransactions.First();
        Assert.Equal(rootUserId, commission.UserId);
        Assert.Equal(50m, commission.Amount); // 5% for level 2
        Assert.Equal(2, commission.Level);
    }

    [Fact]
    public async Task ProcessPaymentConfirmed_WithIdempotency_ShouldNotDuplicateCommissions()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = 1000m;
        var productId = Guid.NewGuid();

        // Setup that payment already processed
        _commissionTransactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(true);

        // Act
        await _commissionService.ProcessPaymentConfirmedAsync(paymentId, userId, amount, productId);

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
    public async Task ProcessPaymentConfirmed_WithNoActiveRuleSet_ShouldNotGenerateCommissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var amount = 1000m;

        var userReferral = UserReferral.CreateRoot(userId);
        
        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(userReferral);
            
        _commissionRuleSetRepositoryMock
            .Setup(x => x.GetActiveRuleSetAsync())
            .ReturnsAsync((CommissionRuleSet)null); // No active rule set
            
        _commissionTransactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);

        // Act
        await _commissionService.ProcessPaymentConfirmedAsync(paymentId, userId, amount, productId);

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
    public async Task ProcessPaymentConfirmed_WithUserNotInMLM_ShouldNotGenerateCommissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var amount = 1000m;

        _userReferralRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync((UserReferral)null); // User not in MLM
            
        _commissionTransactionRepositoryMock
            .Setup(x => x.ExistsBySourcePaymentIdAsync(paymentId))
            .ReturnsAsync(false);

        // Act
        await _commissionService.ProcessPaymentConfirmedAsync(paymentId, userId, amount, productId);

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
    public void UserReferral_CreateRoot_ShouldHaveCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var rootUser = UserReferral.CreateRoot(userId);

        // Assert
        Assert.Equal(userId, rootUser.UserId);
        Assert.Null(rootUser.ReferredBy);
        Assert.Equal(0, rootUser.Level);
        Assert.Equal("/", rootUser.MaterializedPath);
        Assert.True(rootUser.IsActive);
        Assert.NotNull(rootUser.ReferralCode);
        Assert.NotEqual(Guid.Empty, rootUser.Id);
    }

    [Fact]
    public void UserReferral_CreateChild_ShouldHaveCorrectHierarchy()
    {
        // Arrange
        var parentUserId = Guid.NewGuid();
        var childUserId = Guid.NewGuid();
        var parentUser = UserReferral.CreateRoot(parentUserId);

        // Act
        var childUser = UserReferral.Create(childUserId, parentUser);

        // Assert
        Assert.Equal(childUserId, childUser.UserId);
        Assert.Equal(parentUserId, childUser.ReferredBy);
        Assert.Equal(1, childUser.Level);
        Assert.Equal($"/{parentUser.Id}/", childUser.MaterializedPath);
        Assert.True(childUser.IsActive);
        Assert.NotNull(childUser.ReferralCode);
    }

    [Fact]
    public void UserReferral_CreateDeepHierarchy_ShouldMaintainCorrectPath()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var user3Id = Guid.NewGuid();
        var user4Id = Guid.NewGuid();

        var user1 = UserReferral.CreateRoot(user1Id);
        var user2 = UserReferral.Create(user2Id, user1);
        var user3 = UserReferral.Create(user3Id, user2);

        // Act
        var user4 = UserReferral.Create(user4Id, user3);

        // Assert
        Assert.Equal(3, user4.Level);
        Assert.Equal($"/{user1.Id}/{user2.Id}/{user3.Id}/", user4.MaterializedPath);
        Assert.Equal(user3Id, user4.ReferredBy);
    }

    [Fact]
    public void CommissionTransaction_Create_ShouldHaveCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var purchaseAmount = 1000m;
        var level = 1;
        var commissionAmount = 100m;

        // Act
        var transaction = CommissionTransaction.Create(
            userId, paymentId, productId, purchaseAmount, level, commissionAmount
        );

        // Assert
        Assert.Equal(userId, transaction.UserId);
        Assert.Equal(paymentId, transaction.SourcePaymentId);
        Assert.Equal(productId, transaction.ProductId);
        Assert.Equal(purchaseAmount, transaction.PurchaseAmount);
        Assert.Equal(level, transaction.Level);
        Assert.Equal(commissionAmount, transaction.Amount);
        Assert.Equal(CommissionStatus.Pending, transaction.Status);
        Assert.Null(transaction.ApprovedBy);
        Assert.Null(transaction.ApprovedAt);
        Assert.Null(transaction.RejectedBy);
        Assert.Null(transaction.RejectedAt);
    }

    [Fact]
    public void CommissionTransaction_Approve_ShouldUpdateStatusAndProperties()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000m, 1, 100m
        );
        var approvedBy = Guid.NewGuid();
        var notes = "Approved for testing";

        // Act
        transaction.Approve(approvedBy, notes);

        // Assert
        Assert.Equal(CommissionStatus.Approved, transaction.Status);
        Assert.Equal(approvedBy, transaction.ApprovedBy);
        Assert.NotNull(transaction.ApprovedAt);
        Assert.Equal(notes, transaction.Notes);
        Assert.Null(transaction.RejectedBy);
        Assert.Null(transaction.RejectedAt);
    }

    [Fact]
    public void CommissionTransaction_Reject_ShouldUpdateStatusAndProperties()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000m, 1, 100m
        );
        var rejectedBy = Guid.NewGuid();
        var reason = "Rejected for testing";

        // Act
        transaction.Reject(rejectedBy, reason);

        // Assert
        Assert.Equal(CommissionStatus.Rejected, transaction.Status);
        Assert.Equal(rejectedBy, transaction.RejectedBy);
        Assert.NotNull(transaction.RejectedAt);
        Assert.Equal(reason, transaction.Notes);
        Assert.Null(transaction.ApprovedBy);
        Assert.Null(transaction.ApprovedAt);
    }

    [Fact]
    public void CommissionRuleSet_Create_ShouldHaveCorrectInitialState()
    {
        // Arrange
        var name = "Test Rules";
        var description = "Test Description";

        // Act
        var ruleSet = CommissionRuleSet.Create(name, description);

        // Assert
        Assert.Equal(name, ruleSet.Name);
        Assert.Equal(description, ruleSet.Description);
        Assert.False(ruleSet.IsActive);
        Assert.Empty(ruleSet.Versions);
        Assert.NotEqual(Guid.Empty, ruleSet.Id);
    }

    [Fact]
    public void CommissionRuleSet_CreateVersion_ShouldAddVersionCorrectly()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test Rules", "Test Description");
        var schemaJson = "{\"level\": 1, \"percentage\": 10}";
        var description = "Version 1";

        // Act
        var version = ruleSet.CreateVersion(schemaJson, description);

        // Assert
        Assert.Single(ruleSet.Versions);
        Assert.Equal(schemaJson, version.SchemaJson);
        Assert.Equal(description, version.Description);
        Assert.False(version.IsPublished);
        Assert.Equal(ruleSet.Id, version.RuleSetId);
        Assert.Equal(1, version.VersionNumber);
    }

    [Fact]
    public void CommissionRuleSet_CreateMultipleVersions_ShouldIncrementVersionNumber()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test Rules", "Test Description");

        // Act
        var version1 = ruleSet.CreateVersion("{\"v\": 1}", "Version 1");
        var version2 = ruleSet.CreateVersion("{\"v\": 2}", "Version 2");
        var version3 = ruleSet.CreateVersion("{\"v\": 3}", "Version 3");

        // Assert
        Assert.Equal(3, ruleSet.Versions.Count);
        Assert.Equal(1, version1.VersionNumber);
        Assert.Equal(2, version2.VersionNumber);
        Assert.Equal(3, version3.VersionNumber);
    }

    [Fact]
    public void CommissionRuleVersion_Publish_ShouldUpdateStatus()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test Rules", "Test Description");
        var version = ruleSet.CreateVersion("{\"test\": true}", "Test Version");

        // Act
        version.Publish();

        // Assert
        Assert.True(version.IsPublished);
        Assert.NotNull(version.PublishedAt);
    }

    [Fact]
    public void CommissionRuleSet_Activate_ShouldSetActiveStatus()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test Rules", "Test Description");
        var version = ruleSet.CreateVersion("{\"test\": true}", "Test Version");
        version.Publish();

        // Act
        ruleSet.Activate();

        // Assert
        Assert.True(ruleSet.IsActive);
        Assert.NotNull(ruleSet.ActivatedAt);
    }

    [Fact]
    public void CommissionRuleSet_Deactivate_ShouldUnsetActiveStatus()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test Rules", "Test Description");
        var version = ruleSet.CreateVersion("{\"test\": true}", "Test Version");
        version.Publish();
        ruleSet.Activate();

        // Act
        ruleSet.Deactivate();

        // Assert
        Assert.False(ruleSet.IsActive);
        Assert.Null(ruleSet.ActivatedAt);
    }

    [Theory]
    [InlineData(0, "/")]
    [InlineData(1, "/parent/")]
    [InlineData(2, "/grandparent/parent/")]
    [InlineData(3, "/great-grandparent/grandparent/parent/")]
    public void UserReferral_MaterializedPath_ShouldReflectHierarchyLevel(int expectedLevel, string expectedPathPattern)
    {
        // Arrange
        var users = new List<UserReferral>();
        var rootUser = UserReferral.CreateRoot(Guid.NewGuid());
        users.Add(rootUser);

        var currentParent = rootUser;
        for (int i = 1; i <= 3; i++)
        {
            var user = UserReferral.Create(Guid.NewGuid(), currentParent);
            users.Add(user);
            currentParent = user;
        }

        // Act & Assert
        var targetUser = users[expectedLevel];
        Assert.Equal(expectedLevel, targetUser.Level);
        
        // Verify path structure (count of slashes should be level + 1 for non-root)
        if (expectedLevel == 0)
        {
            Assert.Equal("/", targetUser.MaterializedPath);
        }
        else
        {
            var slashCount = targetUser.MaterializedPath.Count(c => c == '/');
            Assert.Equal(expectedLevel + 1, slashCount);
        }
    }

    [Fact]
    public void UserReferral_Deactivate_ShouldSetInactiveStatus()
    {
        // Arrange
        var user = UserReferral.CreateRoot(Guid.NewGuid());
        Assert.True(user.IsActive);

        // Act
        user.Deactivate();

        // Assert
        Assert.False(user.IsActive);
    }

    [Fact]
    public void UserReferral_Activate_ShouldSetActiveStatus()
    {
        // Arrange
        var user = UserReferral.CreateRoot(Guid.NewGuid());
        user.Deactivate();
        Assert.False(user.IsActive);

        // Act
        user.Activate();

        // Assert
        Assert.True(user.IsActive);
    }

    [Fact]
    public void CommissionGeneratedEvent_Create_ShouldHaveCorrectProperties()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = 100m;
        var level = 1;
        var sourcePaymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var domainEvent = new CommissionGeneratedEvent(
            transactionId, userId, amount, level, sourcePaymentId, productId
        );

        // Assert
        Assert.Equal(transactionId, domainEvent.TransactionId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(amount, domainEvent.Amount);
        Assert.Equal(level, domainEvent.Level);
        Assert.Equal(sourcePaymentId, domainEvent.SourcePaymentId);
        Assert.Equal(productId, domainEvent.ProductId);
        Assert.True(domainEvent.OccurredOn <= DateTime.UtcNow);
        Assert.NotEqual(Guid.Empty, domainEvent.Id);
    }

    [Fact]
    public void CommissionApprovedEvent_Create_ShouldHaveCorrectProperties()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = 100m;
        var sourcePaymentId = Guid.NewGuid();
        var approvedBy = Guid.NewGuid();
        var approvedAt = DateTime.UtcNow;

        // Act
        var domainEvent = new CommissionApprovedEvent(
            transactionId, userId, amount, sourcePaymentId, approvedBy, approvedAt
        );

        // Assert
        Assert.Equal(transactionId, domainEvent.TransactionId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(amount, domainEvent.Amount);
        Assert.Equal(sourcePaymentId, domainEvent.SourcePaymentId);
        Assert.Equal(approvedBy, domainEvent.ApprovedBy);
        Assert.Equal(approvedAt, domainEvent.ApprovedAt);
        Assert.True(domainEvent.OccurredOn <= DateTime.UtcNow);
        Assert.NotEqual(Guid.Empty, domainEvent.Id);
    }
}