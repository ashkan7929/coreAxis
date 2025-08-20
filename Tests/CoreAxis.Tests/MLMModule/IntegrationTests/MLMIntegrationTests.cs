using CoreAxis.Modules.MLMModule.Application.Contracts;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Infrastructure.Persistence;
using CoreAxis.SharedKernel.Application.Pagination;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CoreAxis.Tests.MLMModule.IntegrationTests;

public class MLMIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly MLMModuleDbContext _dbContext;
    private readonly Mock<IEventBus> _eventBusMock;

    public MLMIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        var services = new ServiceCollection();
        
        // Setup in-memory database
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<MLMModuleDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));
        
        // Setup mocks
        _eventBusMock = new Mock<IEventBus>();
        services.AddSingleton(_eventBusMock.Object);
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Register MLM services
        services.AddScoped<IMLMService, MLMService>();
        services.AddScoped<ICommissionManagementService, CommissionManagementService>();
        services.AddScoped<CommissionCalculationService>();
        
        // Register repositories
        services.AddScoped<IUserReferralRepository, UserReferralRepository>();
        services.AddScoped<ICommissionRuleSetRepository, CommissionRuleSetRepository>();
        services.AddScoped<ICommissionTransactionRepository, CommissionTransactionRepository>();
        
        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<MLMModuleDbContext>();
        
        // Ensure database is created
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task MLMService_WithRepositoryLayer_ShouldPersistDataCorrectly()
    {
        // Arrange
        var mlmService = _serviceProvider.GetRequiredService<IMLMService>();
        var userId = Guid.NewGuid();
        
        // Act - Join MLM
        var joinRequest = new JoinMLMRequest { ReferralCode = null };
        var referralInfo = await mlmService.JoinMLMAsync(userId, joinRequest);
        
        // Assert - Verify data persisted in database
        var persistedReferral = await _dbContext.UserReferrals
            .FirstOrDefaultAsync(ur => ur.UserId == userId);
        
        Assert.NotNull(persistedReferral);
        Assert.Equal(userId, persistedReferral.UserId);
        Assert.Equal(0, persistedReferral.Level);
        Assert.True(persistedReferral.IsActive);
        Assert.NotNull(persistedReferral.ReferralCode);
        Assert.Equal("/", persistedReferral.MaterializedPath);
        
        // Act - Get referral info through service
        var retrievedInfo = await mlmService.GetUserReferralInfoAsync(userId);
        
        // Assert - Service returns correct data
        Assert.Equal(referralInfo.UserId, retrievedInfo.UserId);
        Assert.Equal(referralInfo.ReferralCode, retrievedInfo.ReferralCode);
        Assert.Equal(referralInfo.Level, retrievedInfo.Level);
        Assert.Equal(referralInfo.IsActive, retrievedInfo.IsActive);
    }

    [Fact]
    public async Task CommissionCalculationService_WithEventBus_ShouldPublishEventsCorrectly()
    {
        // Arrange
        await SetupCommissionRulesAsync();
        var calculationService = _serviceProvider.GetRequiredService<CommissionCalculationService>();
        
        // Create a simple network
        var parentUserId = Guid.NewGuid();
        var childUserId = Guid.NewGuid();
        
        var parentReferral = UserReferral.CreateRoot(parentUserId);
        var childReferral = UserReferral.Create(childUserId, parentReferral);
        
        _dbContext.UserReferrals.AddRange(parentReferral, childReferral);
        await _dbContext.SaveChangesAsync();
        
        // Act - Process payment
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var amount = 1000m;
        
        await calculationService.ProcessPaymentConfirmedAsync(paymentId, childUserId, amount, productId);
        
        // Assert - Verify commissions were created
        var commissions = await _dbContext.CommissionTransactions
            .Where(ct => ct.SourcePaymentId == paymentId)
            .ToListAsync();
        
        Assert.NotEmpty(commissions);
        Assert.Contains(commissions, c => c.UserId == parentUserId);
        
        // Assert - Verify events were published
        _eventBusMock.Verify(
            x => x.PublishAsync(It.IsAny<IDomainEvent>()),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task CommissionManagementService_WithApprovalWorkflow_ShouldUpdateStatusAndPublishEvents()
    {
        // Arrange
        await SetupCommissionRulesAsync();
        var managementService = _serviceProvider.GetRequiredService<ICommissionManagementService>();
        
        // Create commission transaction
        var userId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var commission = CommissionTransaction.Create(
            userId, paymentId, productId, 100m, 1, 10m
        );
        
        _dbContext.CommissionTransactions.Add(commission);
        await _dbContext.SaveChangesAsync();
        
        // Act - Approve commission
        var adminUserId = Guid.NewGuid();
        var approveRequest = new ApproveCommissionRequest
        {
            Notes = "Integration test approval"
        };
        
        await managementService.ApproveCommissionAsync(commission.Id, adminUserId, approveRequest);
        
        // Assert - Verify status updated in database
        var updatedCommission = await _dbContext.CommissionTransactions
            .FirstAsync(ct => ct.Id == commission.Id);
        
        Assert.Equal(CommissionStatus.Approved, updatedCommission.Status);
        Assert.Equal(adminUserId, updatedCommission.ApprovedBy);
        Assert.NotNull(updatedCommission.ApprovedAt);
        Assert.Equal("Integration test approval", updatedCommission.Notes);
        
        // Assert - Verify event was published
        _eventBusMock.Verify(
            x => x.PublishAsync(It.IsAny<IDomainEvent>()),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task UserReferralRepository_WithMaterializedPath_ShouldQueryHierarchyEfficiently()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<IUserReferralRepository>();
        
        // Create a 5-level hierarchy
        var users = new List<UserReferral>();
        var rootUser = UserReferral.CreateRoot(Guid.NewGuid());
        users.Add(rootUser);
        
        var currentParent = rootUser;
        for (int i = 1; i < 5; i++)
        {
            var user = UserReferral.Create(Guid.NewGuid(), currentParent);
            users.Add(user);
            currentParent = user;
        }
        
        _dbContext.UserReferrals.AddRange(users);
        await _dbContext.SaveChangesAsync();
        
        // Act & Assert - Test upline queries
        var leafUser = users.Last();
        var uplineUsers = await repository.GetUplineUsersAsync(leafUser.UserId);
        
        Assert.Equal(4, uplineUsers.Count); // All users except the leaf
        Assert.Contains(uplineUsers, u => u.UserId == rootUser.UserId);
        
        // Act & Assert - Test downline queries
        var downlineUsers = await repository.GetDownlineUsersAsync(rootUser.UserId, 1, 10, null);
        
        Assert.Equal(4, downlineUsers.TotalCount); // All users except the root
        Assert.Contains(downlineUsers.Items, u => u.UserId == leafUser.UserId);
        
        // Act & Assert - Test direct children
        var directChildren = await repository.GetDirectChildrenAsync(rootUser.UserId, 1, 10);
        
        Assert.Equal(1, directChildren.TotalCount); // Only immediate child
        Assert.Equal(users[1].UserId, directChildren.Items.First().UserId);
        
        // Act & Assert - Test users at specific level
        var level3Users = await repository.GetUsersAtLevelAsync(3, 1, 10);
        
        Assert.Equal(1, level3Users.TotalCount);
        Assert.Equal(users[3].UserId, level3Users.Items.First().UserId);
    }

    [Fact]
    public async Task CommissionRuleSetRepository_WithVersioning_ShouldMaintainVersionHistory()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ICommissionRuleSetRepository>();
        
        var ruleSet = CommissionRuleSet.Create(
            "Integration Test Rules",
            "Rules for integration testing"
        );
        
        // Create multiple versions
        var version1 = ruleSet.CreateVersion(
            "{\"levels\": [{\"level\": 1, \"percentage\": 10}]}",
            "Version 1"
        );
        version1.Publish();
        
        var version2 = ruleSet.CreateVersion(
            "{\"levels\": [{\"level\": 1, \"percentage\": 15}]}",
            "Version 2"
        );
        version2.Publish();
        
        ruleSet.Activate();
        
        _dbContext.CommissionRuleSets.Add(ruleSet);
        await _dbContext.SaveChangesAsync();
        
        // Act - Get active rule set
        var activeRuleSet = await repository.GetActiveRuleSetAsync();
        
        // Assert
        Assert.NotNull(activeRuleSet);
        Assert.Equal(ruleSet.Id, activeRuleSet.Id);
        Assert.True(activeRuleSet.IsActive);
        
        // Act - Get all versions
        var allVersions = await repository.GetRuleSetVersionsAsync(ruleSet.Id);
        
        // Assert
        Assert.Equal(2, allVersions.Count);
        Assert.All(allVersions, v => Assert.True(v.IsPublished));
        
        // Act - Get latest version
        var latestVersion = await repository.GetLatestVersionAsync(ruleSet.Id);
        
        // Assert
        Assert.NotNull(latestVersion);
        Assert.Equal(version2.Id, latestVersion.Id);
        Assert.Equal("Version 2", latestVersion.Description);
    }

    [Fact]
    public async Task CommissionTransactionRepository_WithFiltering_ShouldReturnCorrectResults()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ICommissionTransactionRepository>();
        
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var paymentId1 = Guid.NewGuid();
        var paymentId2 = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var commissions = new[]
        {
            CommissionTransaction.Create(userId1, paymentId1, productId, 100m, 1, 10m),
            CommissionTransaction.Create(userId1, paymentId2, productId, 200m, 2, 10m),
            CommissionTransaction.Create(userId2, paymentId1, productId, 150m, 1, 15m)
        };
        
        // Approve one commission
        commissions[0].Approve(Guid.NewGuid(), "Test approval");
        
        _dbContext.CommissionTransactions.AddRange(commissions);
        await _dbContext.SaveChangesAsync();
        
        // Act & Assert - Filter by user
        var user1Commissions = await repository.GetUserCommissionsAsync(
            userId1, null, null, null, 1, 10
        );
        
        Assert.Equal(2, user1Commissions.TotalCount);
        Assert.All(user1Commissions.Items, c => Assert.Equal(userId1, c.UserId));
        
        // Act & Assert - Filter by status
        var pendingCommissions = await repository.GetUserCommissionsAsync(
            userId1, CommissionStatus.Pending, null, null, 1, 10
        );
        
        Assert.Equal(1, pendingCommissions.TotalCount);
        Assert.Equal(CommissionStatus.Pending, pendingCommissions.Items.First().Status);
        
        // Act & Assert - Filter by date range
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        var todayCommissions = await repository.GetUserCommissionsAsync(
            userId1, null, today, tomorrow, 1, 10
        );
        
        Assert.Equal(2, todayCommissions.TotalCount);
        
        // Act & Assert - Get pending commissions for management
        var allPendingCommissions = await repository.GetPendingCommissionsAsync(1, 10);
        
        Assert.Equal(2, allPendingCommissions.TotalCount); // 2 pending commissions total
        Assert.All(allPendingCommissions.Items, c => Assert.Equal(CommissionStatus.Pending, c.Status));
    }

    [Fact]
    public async Task MLMService_WithPagination_ShouldReturnCorrectPagedResults()
    {
        // Arrange
        var mlmService = _serviceProvider.GetRequiredService<IMLMService>();
        
        // Create a network with many users
        var rootUserId = Guid.NewGuid();
        var rootJoinRequest = new JoinMLMRequest { ReferralCode = null };
        var rootReferralInfo = await mlmService.JoinMLMAsync(rootUserId, rootJoinRequest);
        
        // Create 25 direct children
        var childUserIds = new List<Guid>();
        for (int i = 0; i < 25; i++)
        {
            var childUserId = Guid.NewGuid();
            childUserIds.Add(childUserId);
            
            var childJoinRequest = new JoinMLMRequest { ReferralCode = rootReferralInfo.ReferralCode };
            await mlmService.JoinMLMAsync(childUserId, childJoinRequest);
        }
        
        // Act - Test pagination
        var page1Request = new GetDownlineRequest { PageNumber = 1, PageSize = 10, MaxDepth = 1 };
        var page1 = await mlmService.GetUserDownlineAsync(rootUserId, page1Request);
        
        var page2Request = new GetDownlineRequest { PageNumber = 2, PageSize = 10, MaxDepth = 1 };
        var page2 = await mlmService.GetUserDownlineAsync(rootUserId, page2Request);
        
        var page3Request = new GetDownlineRequest { PageNumber = 3, PageSize = 10, MaxDepth = 1 };
        var page3 = await mlmService.GetUserDownlineAsync(rootUserId, page3Request);
        
        // Assert
        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count());
        Assert.Equal(1, page1.PageNumber);
        Assert.Equal(3, page1.TotalPages);
        
        Assert.Equal(25, page2.TotalCount);
        Assert.Equal(10, page2.Items.Count());
        Assert.Equal(2, page2.PageNumber);
        
        Assert.Equal(25, page3.TotalCount);
        Assert.Equal(5, page3.Items.Count()); // Last page has remaining items
        Assert.Equal(3, page3.PageNumber);
        
        // Verify no duplicate users across pages
        var allUserIds = page1.Items.Concat(page2.Items).Concat(page3.Items)
            .Select(u => u.UserId)
            .ToList();
        
        Assert.Equal(25, allUserIds.Distinct().Count());
    }

    [Fact]
    public async Task EventBusIntegration_WithDomainEvents_ShouldPublishCorrectEventTypes()
    {
        // Arrange
        await SetupCommissionRulesAsync();
        var calculationService = _serviceProvider.GetRequiredService<CommissionCalculationService>();
        var managementService = _serviceProvider.GetRequiredService<ICommissionManagementService>();
        
        // Create network and commission
        var parentUserId = Guid.NewGuid();
        var childUserId = Guid.NewGuid();
        
        var parentReferral = UserReferral.CreateRoot(parentUserId);
        var childReferral = UserReferral.Create(childUserId, parentReferral);
        
        _dbContext.UserReferrals.AddRange(parentReferral, childReferral);
        await _dbContext.SaveChangesAsync();
        
        // Act - Process payment (should generate CommissionGenerated events)
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        await calculationService.ProcessPaymentConfirmedAsync(paymentId, childUserId, 1000m, productId);
        
        // Get the generated commission
        var commission = await _dbContext.CommissionTransactions
            .FirstAsync(ct => ct.SourcePaymentId == paymentId);
        
        // Act - Approve commission (should generate CommissionApproved event)
        var adminUserId = Guid.NewGuid();
        var approveRequest = new ApproveCommissionRequest { Notes = "Test approval" };
        
        await managementService.ApproveCommissionAsync(commission.Id, adminUserId, approveRequest);
        
        // Assert - Verify events were published
        _eventBusMock.Verify(
            x => x.PublishAsync(It.IsAny<IDomainEvent>()),
            Times.AtLeast(2) // At least CommissionGenerated + CommissionApproved
        );
        
        // Verify the events contain correct data
        var publishedEvents = _eventBusMock.Invocations
            .Where(i => i.Method.Name == nameof(IEventBus.PublishAsync))
            .Select(i => i.Arguments[0] as IDomainEvent)
            .ToList();
        
        Assert.NotEmpty(publishedEvents);
        Assert.All(publishedEvents, e => Assert.NotNull(e));
    }

    private async Task SetupCommissionRulesAsync()
    {
        var ruleSet = CommissionRuleSet.Create(
            "Integration Test Rules",
            "Commission rules for integration testing"
        );
        
        var schemaJson = """
        {
            "levels": [
                { "level": 1, "percentage": 10 },
                { "level": 2, "percentage": 5 }
            ]
        }
        """;
        
        var version = ruleSet.CreateVersion(schemaJson, "Integration test version");
        version.Publish();
        ruleSet.Activate();
        
        _dbContext.CommissionRuleSets.Add(ruleSet);
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}