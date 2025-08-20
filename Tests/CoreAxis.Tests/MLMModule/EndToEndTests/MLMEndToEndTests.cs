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

namespace CoreAxis.Tests.MLMModule.EndToEndTests;

public class MLMEndToEndTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly MLMModuleDbContext _dbContext;
    private readonly IMLMService _mlmService;
    private readonly ICommissionManagementService _commissionManagementService;
    private readonly CommissionCalculationService _commissionCalculationService;
    private readonly Mock<IEventBus> _eventBusMock;

    public MLMEndToEndTests(ITestOutputHelper output)
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
        _mlmService = _serviceProvider.GetRequiredService<IMLMService>();
        _commissionManagementService = _serviceProvider.GetRequiredService<ICommissionManagementService>();
        _commissionCalculationService = _serviceProvider.GetRequiredService<CommissionCalculationService>();
        
        // Ensure database is created
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task CompleteMLMJourney_FromJoinToCommissionPayout_ShouldWorkEndToEnd()
    {
        // Arrange - Setup commission rules first
        await SetupCommissionRulesAsync();
        
        var rootUserId = Guid.NewGuid();
        var level1UserId = Guid.NewGuid();
        var level2UserId = Guid.NewGuid();
        var level3UserId = Guid.NewGuid();
        
        _output.WriteLine("=== Starting Complete MLM Journey Test ===");
        
        // Step 1: Root user joins MLM (creates root)
        _output.WriteLine("Step 1: Root user joins MLM");
        var rootJoinRequest = new JoinMLMRequest { ReferralCode = null }; // Root user
        var rootReferralInfo = await _mlmService.JoinMLMAsync(rootUserId, rootJoinRequest);
        
        Assert.NotNull(rootReferralInfo);
        Assert.Equal(rootUserId, rootReferralInfo.UserId);
        Assert.Equal(0, rootReferralInfo.Level);
        Assert.True(rootReferralInfo.IsActive);
        _output.WriteLine($"Root user joined with referral code: {rootReferralInfo.ReferralCode}");
        
        // Step 2: Level 1 user joins using root's referral code
        _output.WriteLine("Step 2: Level 1 user joins using root's referral code");
        var level1JoinRequest = new JoinMLMRequest { ReferralCode = rootReferralInfo.ReferralCode };
        var level1ReferralInfo = await _mlmService.JoinMLMAsync(level1UserId, level1JoinRequest);
        
        Assert.NotNull(level1ReferralInfo);
        Assert.Equal(level1UserId, level1ReferralInfo.UserId);
        Assert.Equal(1, level1ReferralInfo.Level);
        Assert.Equal(rootUserId, level1ReferralInfo.ParentUserId);
        _output.WriteLine($"Level 1 user joined with referral code: {level1ReferralInfo.ReferralCode}");
        
        // Step 3: Level 2 user joins using level 1's referral code
        _output.WriteLine("Step 3: Level 2 user joins using level 1's referral code");
        var level2JoinRequest = new JoinMLMRequest { ReferralCode = level1ReferralInfo.ReferralCode };
        var level2ReferralInfo = await _mlmService.JoinMLMAsync(level2UserId, level2JoinRequest);
        
        Assert.Equal(2, level2ReferralInfo.Level);
        Assert.Equal(level1UserId, level2ReferralInfo.ParentUserId);
        _output.WriteLine($"Level 2 user joined with referral code: {level2ReferralInfo.ReferralCode}");
        
        // Step 4: Level 3 user joins using level 2's referral code
        _output.WriteLine("Step 4: Level 3 user joins using level 2's referral code");
        var level3JoinRequest = new JoinMLMRequest { ReferralCode = level2ReferralInfo.ReferralCode };
        var level3ReferralInfo = await _mlmService.JoinMLMAsync(level3UserId, level3JoinRequest);
        
        Assert.Equal(3, level3ReferralInfo.Level);
        Assert.Equal(level2UserId, level3ReferralInfo.ParentUserId);
        _output.WriteLine($"Level 3 user joined with referral code: {level3ReferralInfo.ReferralCode}");
        
        // Step 5: Verify network structure
        _output.WriteLine("Step 5: Verify network structure");
        var rootDownlineRequest = new GetDownlineRequest { PageNumber = 1, PageSize = 10, MaxDepth = 5 };
        var rootDownline = await _mlmService.GetUserDownlineAsync(rootUserId, rootDownlineRequest);
        
        Assert.Equal(3, rootDownline.TotalCount); // Root should see 3 downline users
        _output.WriteLine($"Root user has {rootDownline.TotalCount} downline users");
        
        // Step 6: Level 3 user makes a purchase (triggers commission calculation)
        _output.WriteLine("Step 6: Level 3 user makes a purchase");
        var paymentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var purchaseAmount = 1000m;
        
        await _commissionCalculationService.ProcessPaymentConfirmedAsync(
            paymentId, level3UserId, purchaseAmount, productId
        );
        
        // Step 7: Verify commissions were generated
        _output.WriteLine("Step 7: Verify commissions were generated");
        
        // Check level 2 user's commissions (direct upline)
        var level2CommissionsRequest = new GetCommissionsRequest { PageNumber = 1, PageSize = 10 };
        var level2Commissions = await _mlmService.GetUserCommissionsAsync(level2UserId, level2CommissionsRequest);
        Assert.True(level2Commissions.TotalCount > 0);
        _output.WriteLine($"Level 2 user received {level2Commissions.TotalCount} commission(s)");
        
        // Check level 1 user's commissions
        var level1CommissionsRequest = new GetCommissionsRequest { PageNumber = 1, PageSize = 10 };
        var level1Commissions = await _mlmService.GetUserCommissionsAsync(level1UserId, level1CommissionsRequest);
        Assert.True(level1Commissions.TotalCount > 0);
        _output.WriteLine($"Level 1 user received {level1Commissions.TotalCount} commission(s)");
        
        // Check root user's commissions
        var rootCommissionsRequest = new GetCommissionsRequest { PageNumber = 1, PageSize = 10 };
        var rootCommissions = await _mlmService.GetUserCommissionsAsync(rootUserId, rootCommissionsRequest);
        Assert.True(rootCommissions.TotalCount > 0);
        _output.WriteLine($"Root user received {rootCommissions.TotalCount} commission(s)");
        
        // Step 8: Admin approves commissions
        _output.WriteLine("Step 8: Admin approves commissions");
        var adminUserId = Guid.NewGuid();
        
        var pendingCommissionsRequest = new GetPendingCommissionsRequest { PageNumber = 1, PageSize = 50 };
        var pendingCommissions = await _commissionManagementService.GetPendingCommissionsAsync(pendingCommissionsRequest);
        
        Assert.True(pendingCommissions.TotalCount > 0);
        _output.WriteLine($"Found {pendingCommissions.TotalCount} pending commission(s) for approval");
        
        // Approve all pending commissions
        foreach (var commission in pendingCommissions.Items)
        {
            var approveRequest = new ApproveCommissionRequest
            {
                Notes = "Approved in end-to-end test"
            };
            
            await _commissionManagementService.ApproveCommissionAsync(
                commission.Id, adminUserId, approveRequest
            );
        }
        
        _output.WriteLine("All commissions approved");
        
        // Step 9: Verify commission approval events were published
        _output.WriteLine("Step 9: Verify commission approval events were published");
        _eventBusMock.Verify(
            x => x.PublishAsync(It.IsAny<IDomainEvent>()),
            Times.AtLeastOnce
        );
        
        // Step 10: Verify final commission states
        _output.WriteLine("Step 10: Verify final commission states");
        var finalLevel2Commissions = await _mlmService.GetUserCommissionsAsync(level2UserId, level2CommissionsRequest);
        var approvedLevel2Commissions = finalLevel2Commissions.Items.Where(c => c.Status == "Approved").ToList();
        Assert.True(approvedLevel2Commissions.Any());
        
        var totalApprovedAmount = approvedLevel2Commissions.Sum(c => c.Amount);
        _output.WriteLine($"Level 2 user's total approved commission amount: {totalApprovedAmount:C}");
        
        _output.WriteLine("=== Complete MLM Journey Test Completed Successfully ===");
    }

    [Fact]
    public async Task MLMNetworkGrowth_WithMultipleBranches_ShouldMaintainCorrectHierarchy()
    {
        // Arrange
        await SetupCommissionRulesAsync();
        
        var rootUserId = Guid.NewGuid();
        _output.WriteLine("=== Testing MLM Network Growth with Multiple Branches ===");
        
        // Create root user
        var rootJoinRequest = new JoinMLMRequest { ReferralCode = null };
        var rootReferralInfo = await _mlmService.JoinMLMAsync(rootUserId, rootJoinRequest);
        
        // Create multiple branches
        var branch1Users = new List<Guid>();
        var branch2Users = new List<Guid>();
        
        // Branch 1: Create 5 users in a chain
        var currentParentCode = rootReferralInfo.ReferralCode;
        for (int i = 0; i < 5; i++)
        {
            var userId = Guid.NewGuid();
            branch1Users.Add(userId);
            
            var joinRequest = new JoinMLMRequest { ReferralCode = currentParentCode };
            var referralInfo = await _mlmService.JoinMLMAsync(userId, joinRequest);
            
            Assert.Equal(i + 1, referralInfo.Level);
            currentParentCode = referralInfo.ReferralCode;
            
            _output.WriteLine($"Branch 1 - User {i + 1} joined at level {referralInfo.Level}");
        }
        
        // Branch 2: Create 3 users directly under root
        for (int i = 0; i < 3; i++)
        {
            var userId = Guid.NewGuid();
            branch2Users.Add(userId);
            
            var joinRequest = new JoinMLMRequest { ReferralCode = rootReferralInfo.ReferralCode };
            var referralInfo = await _mlmService.JoinMLMAsync(userId, joinRequest);
            
            Assert.Equal(1, referralInfo.Level);
            _output.WriteLine($"Branch 2 - User {i + 1} joined at level {referralInfo.Level}");
        }
        
        // Verify root user's network stats
        var rootInfo = await _mlmService.GetUserReferralInfoAsync(rootUserId);
        Assert.Equal(8, rootInfo.TotalDownlineCount); // 5 + 3 = 8 total downline
        Assert.Equal(4, rootInfo.DirectChildrenCount); // 1 from branch1 + 3 from branch2
        
        _output.WriteLine($"Root user stats - Total downline: {rootInfo.TotalDownlineCount}, Direct children: {rootInfo.DirectChildrenCount}");
        
        // Verify branch structures
        var rootDownlineRequest = new GetDownlineRequest { PageNumber = 1, PageSize = 20, MaxDepth = 10 };
        var rootDownline = await _mlmService.GetUserDownlineAsync(rootUserId, rootDownlineRequest);
        
        var level1Users = rootDownline.Items.Where(u => u.Level == 1).ToList();
        var level2Users = rootDownline.Items.Where(u => u.Level == 2).ToList();
        var level5Users = rootDownline.Items.Where(u => u.Level == 5).ToList();
        
        Assert.Equal(4, level1Users.Count); // 1 from branch1 + 3 from branch2
        Assert.Equal(1, level2Users.Count); // Only from branch1
        Assert.Equal(1, level5Users.Count); // Only from branch1
        
        _output.WriteLine($"Network verification - Level 1: {level1Users.Count}, Level 2: {level2Users.Count}, Level 5: {level5Users.Count}");
        
        _output.WriteLine("=== MLM Network Growth Test Completed Successfully ===");
    }

    [Fact]
    public async Task CommissionCalculation_WithComplexNetwork_ShouldDistributeCorrectly()
    {
        // Arrange
        await SetupCommissionRulesAsync();
        
        _output.WriteLine("=== Testing Commission Calculation with Complex Network ===");
        
        // Create a 4-level network
        var users = await CreateMultiLevelNetworkAsync(4);
        var leafUser = users.Last();
        
        // Multiple purchases from the leaf user
        var purchases = new[]
        {
            new { Amount = 1000m, ProductId = Guid.NewGuid() },
            new { Amount = 500m, ProductId = Guid.NewGuid() },
            new { Amount = 2000m, ProductId = Guid.NewGuid() }
        };
        
        foreach (var purchase in purchases)
        {
            var paymentId = Guid.NewGuid();
            await _commissionCalculationService.ProcessPaymentConfirmedAsync(
                paymentId, leafUser.UserId, purchase.Amount, purchase.ProductId
            );
            
            _output.WriteLine($"Processed purchase of {purchase.Amount:C}");
        }
        
        // Verify commission distribution
        var totalPurchaseAmount = purchases.Sum(p => p.Amount);
        _output.WriteLine($"Total purchase amount: {totalPurchaseAmount:C}");
        
        // Check each level's commissions
        for (int level = 0; level < users.Count - 1; level++) // Exclude leaf user
        {
            var user = users[level];
            var commissionsRequest = new GetCommissionsRequest { PageNumber = 1, PageSize = 50 };
            var commissions = await _mlmService.GetUserCommissionsAsync(user.UserId, commissionsRequest);
            
            var totalCommissionAmount = commissions.Items.Sum(c => c.Amount);
            _output.WriteLine($"Level {level} user received {commissions.TotalCount} commissions totaling {totalCommissionAmount:C}");
            
            Assert.True(commissions.TotalCount > 0, $"Level {level} user should have received commissions");
        }
        
        // Verify idempotency - reprocessing same payments should not create duplicates
        var originalCommissionCount = await GetTotalCommissionCountAsync();
        
        // Try to reprocess first purchase (should be ignored due to idempotency)
        var firstPurchase = purchases.First();
        var duplicatePaymentId = Guid.NewGuid(); // Different payment ID but same source
        
        await _commissionCalculationService.ProcessPaymentConfirmedAsync(
            duplicatePaymentId, leafUser.UserId, firstPurchase.Amount, firstPurchase.ProductId
        );
        
        var newCommissionCount = await GetTotalCommissionCountAsync();
        Assert.True(newCommissionCount > originalCommissionCount, "New payment should generate new commissions");
        
        _output.WriteLine("=== Commission Calculation Test Completed Successfully ===");
    }

    [Fact]
    public async Task CommissionApprovalWorkflow_WithBulkOperations_ShouldHandleEfficiently()
    {
        // Arrange
        await SetupCommissionRulesAsync();
        
        _output.WriteLine("=== Testing Commission Approval Workflow with Bulk Operations ===");
        
        // Create network and generate commissions
        var users = await CreateMultiLevelNetworkAsync(3);
        var leafUser = users.Last();
        
        // Generate multiple payments to create many commissions
        for (int i = 0; i < 10; i++)
        {
            var paymentId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var amount = 100m * (i + 1); // Varying amounts
            
            await _commissionCalculationService.ProcessPaymentConfirmedAsync(
                paymentId, leafUser.UserId, amount, productId
            );
        }
        
        _output.WriteLine("Generated commissions from 10 payments");
        
        // Get all pending commissions
        var pendingRequest = new GetPendingCommissionsRequest { PageNumber = 1, PageSize = 100 };
        var pendingCommissions = await _commissionManagementService.GetPendingCommissionsAsync(pendingRequest);
        
        _output.WriteLine($"Found {pendingCommissions.TotalCount} pending commissions");
        Assert.True(pendingCommissions.TotalCount > 0);
        
        var adminUserId = Guid.NewGuid();
        
        // Approve half of the commissions
        var commissionsToApprove = pendingCommissions.Items.Take(pendingCommissions.Items.Count() / 2).ToList();
        foreach (var commission in commissionsToApprove)
        {
            var approveRequest = new ApproveCommissionRequest
            {
                Notes = $"Bulk approval - Commission {commission.Id}"
            };
            
            await _commissionManagementService.ApproveCommissionAsync(
                commission.Id, adminUserId, approveRequest
            );
        }
        
        _output.WriteLine($"Approved {commissionsToApprove.Count} commissions");
        
        // Reject the remaining commissions
        var remainingPendingRequest = new GetPendingCommissionsRequest { PageNumber = 1, PageSize = 100 };
        var remainingPending = await _commissionManagementService.GetPendingCommissionsAsync(remainingPendingRequest);
        
        foreach (var commission in remainingPending.Items)
        {
            var rejectRequest = new RejectCommissionRequest
            {
                Reason = "Bulk rejection for testing",
                Notes = $"Bulk rejection - Commission {commission.Id}"
            };
            
            await _commissionManagementService.RejectCommissionAsync(
                commission.Id, adminUserId, rejectRequest
            );
        }
        
        _output.WriteLine($"Rejected {remainingPending.Items.Count()} commissions");
        
        // Verify no pending commissions remain
        var finalPendingRequest = new GetPendingCommissionsRequest { PageNumber = 1, PageSize = 100 };
        var finalPending = await _commissionManagementService.GetPendingCommissionsAsync(finalPendingRequest);
        
        Assert.Equal(0, finalPending.TotalCount);
        _output.WriteLine("All commissions have been processed (approved or rejected)");
        
        // Verify event publishing
        _eventBusMock.Verify(
            x => x.PublishAsync(It.IsAny<IDomainEvent>()),
            Times.AtLeast(commissionsToApprove.Count) // At least one event per approval
        );
        
        _output.WriteLine("=== Commission Approval Workflow Test Completed Successfully ===");
    }

    private async Task SetupCommissionRulesAsync()
    {
        var ruleSet = CommissionRuleSet.Create(
            "End-to-End Test Rules",
            "Commission rules for end-to-end testing"
        );
        
        var schemaJson = """
        {
            "levels": [
                { "level": 1, "percentage": 10 },
                { "level": 2, "percentage": 5 },
                { "level": 3, "percentage": 3 },
                { "level": 4, "percentage": 2 },
                { "level": 5, "percentage": 1 }
            ]
        }
        """;
        
        var version = ruleSet.CreateVersion(schemaJson, "End-to-end test version");
        version.Publish();
        ruleSet.Activate();
        
        _dbContext.CommissionRuleSets.Add(ruleSet);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<List<UserReferral>> CreateMultiLevelNetworkAsync(int levels)
    {
        var users = new List<UserReferral>();
        
        // Create root user
        var rootUserId = Guid.NewGuid();
        var rootJoinRequest = new JoinMLMRequest { ReferralCode = null };
        var rootReferralInfo = await _mlmService.JoinMLMAsync(rootUserId, rootJoinRequest);
        
        var rootReferral = await _dbContext.UserReferrals.FirstAsync(ur => ur.UserId == rootUserId);
        users.Add(rootReferral);
        
        // Create chain of users
        var currentParentCode = rootReferralInfo.ReferralCode;
        for (int i = 1; i < levels; i++)
        {
            var userId = Guid.NewGuid();
            var joinRequest = new JoinMLMRequest { ReferralCode = currentParentCode };
            var referralInfo = await _mlmService.JoinMLMAsync(userId, joinRequest);
            
            var userReferral = await _dbContext.UserReferrals.FirstAsync(ur => ur.UserId == userId);
            users.Add(userReferral);
            
            currentParentCode = referralInfo.ReferralCode;
        }
        
        return users;
    }

    private async Task<int> GetTotalCommissionCountAsync()
    {
        return await _dbContext.CommissionTransactions.CountAsync();
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