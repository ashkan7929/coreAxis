using CoreAxis.Modules.MLMModule.Application.Contracts;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Infrastructure.Persistence;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using CoreAxis.EventBus;

namespace CoreAxis.Tests.MLMModule.PerformanceTests;

public class MLMPerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly MLMModuleDbContext _dbContext;
    private readonly Mock<IEventBus> _eventBusMock;

    public MLMPerformanceTests(ITestOutputHelper output)
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
    public async Task MLMNetworkCreation_With10000Users_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        var mlmService = _serviceProvider.GetRequiredService<IMLMService>();
        const int userCount = 10000;
        const int maxExecutionTimeMs = 30000; // 30 seconds
        
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Create root user
        var rootUserId = Guid.NewGuid();
        var rootJoinRequest = new JoinMLMRequest { ReferralCode = null };
        var rootReferralInfo = await mlmService.JoinMLMAsync(rootUserId, rootJoinRequest);
        
        // Create users in batches to simulate realistic growth
        var batchSize = 100;
        var userIds = new List<Guid>();
        var referralCodes = new List<string> { rootReferralInfo.ReferralCode };
        
        for (int batch = 0; batch < userCount / batchSize; batch++)
        {
            var batchTasks = new List<Task>();
            var newReferralCodes = new List<string>();
            
            for (int i = 0; i < batchSize; i++)
            {
                var userId = Guid.NewGuid();
                userIds.Add(userId);
                
                // Randomly select a referral code from existing users
                var randomReferralCode = referralCodes[Random.Shared.Next(referralCodes.Count)];
                var joinRequest = new JoinMLMRequest { ReferralCode = randomReferralCode };
                
                batchTasks.Add(Task.Run(async () =>
                {
                    var referralInfo = await mlmService.JoinMLMAsync(userId, joinRequest);
                    lock (newReferralCodes)
                    {
                        newReferralCodes.Add(referralInfo.ReferralCode);
                    }
                }));
            }
            
            await Task.WhenAll(batchTasks);
            referralCodes.AddRange(newReferralCodes);
            
            _output.WriteLine($"Completed batch {batch + 1}/{userCount / batchSize}, Total users: {(batch + 1) * batchSize + 1}");
        }
        
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxExecutionTimeMs, 
            $"Network creation took {stopwatch.ElapsedMilliseconds}ms, expected < {maxExecutionTimeMs}ms");
        
        // Verify data integrity
        var totalUsers = await _dbContext.UserReferrals.CountAsync();
        Assert.Equal(userCount + 1, totalUsers); // +1 for root user
        
        _output.WriteLine($"Successfully created {totalUsers} users in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per user: {(double)stopwatch.ElapsedMilliseconds / totalUsers:F2}ms");
    }

    [Fact]
    public async Task MaterializedPathQueries_WithLargeHierarchy_ShouldPerformEfficiently()
    {
        // Arrange
        await SetupLargeHierarchyAsync();
        var repository = _serviceProvider.GetRequiredService<IUserReferralRepository>();
        const int maxQueryTimeMs = 1000; // 1 second per query
        
        // Get a random user from level 5
        var level5Users = await _dbContext.UserReferrals
            .Where(ur => ur.Level == 5)
            .Take(10)
            .ToListAsync();
        
        Assert.NotEmpty(level5Users);
        var testUser = level5Users.First();
        
        // Test upline query performance
        var stopwatch = Stopwatch.StartNew();
        var uplineUsers = await repository.GetUplineUsersAsync(testUser.UserId);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < maxQueryTimeMs,
            $"Upline query took {stopwatch.ElapsedMilliseconds}ms, expected < {maxQueryTimeMs}ms");
        Assert.Equal(5, uplineUsers.Count); // Should have 5 upline users
        
        _output.WriteLine($"Upline query for user at level 5: {stopwatch.ElapsedMilliseconds}ms");
        
        // Test downline query performance
        var rootUser = uplineUsers.Last(); // Root user is at the end
        
        stopwatch.Restart();
        var downlineUsers = await repository.GetDownlineUsersAsync(rootUser.UserId, 1, 100, 3);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < maxQueryTimeMs,
            $"Downline query took {stopwatch.ElapsedMilliseconds}ms, expected < {maxQueryTimeMs}ms");
        
        _output.WriteLine($"Downline query (max depth 3): {stopwatch.ElapsedMilliseconds}ms");
        
        // Test direct children query performance
        stopwatch.Restart();
        var directChildren = await repository.GetDirectChildrenAsync(rootUser.UserId, 1, 50);
        stopwatch.Stop();
        
        Assert.True(stopwatch.ElapsedMilliseconds < maxQueryTimeMs,
            $"Direct children query took {stopwatch.ElapsedMilliseconds}ms, expected < {maxQueryTimeMs}ms");
        
        _output.WriteLine($"Direct children query: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CommissionCalculation_WithLargeNetwork_ShouldProcessEfficiently()
    {
        // Arrange
        await SetupCommissionRulesAsync();
        await SetupLargeHierarchyAsync();
        
        var calculationService = _serviceProvider.GetRequiredService<CommissionCalculationService>();
        const int maxProcessingTimeMs = 5000; // 5 seconds
        
        // Get users from different levels for testing
        var level5Users = await _dbContext.UserReferrals
            .Where(ur => ur.Level == 5)
            .Take(10)
            .ToListAsync();
        
        var paymentTasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();
        
        // Process multiple payments simultaneously
        foreach (var user in level5Users)
        {
            var paymentId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var amount = 1000m;
            
            paymentTasks.Add(Task.Run(async () =>
            {
                await calculationService.ProcessPaymentConfirmedAsync(paymentId, user.UserId, amount, productId);
            }));
        }
        
        await Task.WhenAll(paymentTasks);
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxProcessingTimeMs,
            $"Commission calculation took {stopwatch.ElapsedMilliseconds}ms, expected < {maxProcessingTimeMs}ms");
        
        // Verify commissions were created
        var totalCommissions = await _dbContext.CommissionTransactions.CountAsync();
        Assert.True(totalCommissions > 0);
        
        _output.WriteLine($"Processed {level5Users.Count} payments in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Generated {totalCommissions} commission transactions");
        _output.WriteLine($"Average processing time per payment: {(double)stopwatch.ElapsedMilliseconds / level5Users.Count:F2}ms");
    }

    [Fact]
    public async Task BulkCommissionApproval_With1000Commissions_ShouldCompleteQuickly()
    {
        // Arrange
        await SetupCommissionRulesAsync();
        await CreateBulkCommissionsAsync(1000);
        
        var managementService = _serviceProvider.GetRequiredService<ICommissionManagementService>();
        const int maxApprovalTimeMs = 10000; // 10 seconds
        
        // Get pending commissions
        var pendingCommissions = await _dbContext.CommissionTransactions
            .Where(ct => ct.Status == CommissionStatus.Pending)
            .Take(1000)
            .ToListAsync();
        
        Assert.True(pendingCommissions.Count >= 500, "Should have at least 500 pending commissions for testing");
        
        var adminUserId = Guid.NewGuid();
        var approvalTasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();
        
        // Process approvals in batches
        var batchSize = 50;
        for (int i = 0; i < pendingCommissions.Count; i += batchSize)
        {
            var batch = pendingCommissions.Skip(i).Take(batchSize);
            
            foreach (var commission in batch)
            {
                var approveRequest = new ApproveCommissionRequest
                {
                    Notes = $"Bulk approval - Performance test"
                };
                
                approvalTasks.Add(Task.Run(async () =>
                {
                    await managementService.ApproveCommissionAsync(commission.Id, adminUserId, approveRequest);
                }));
            }
            
            // Process batch and wait
            await Task.WhenAll(approvalTasks.Skip(i).Take(Math.Min(batchSize, approvalTasks.Count - i)));
            
            _output.WriteLine($"Processed batch {i / batchSize + 1}, approved {Math.Min(i + batchSize, pendingCommissions.Count)} commissions");
        }
        
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxApprovalTimeMs,
            $"Bulk approval took {stopwatch.ElapsedMilliseconds}ms, expected < {maxApprovalTimeMs}ms");
        
        // Verify approvals
        var approvedCount = await _dbContext.CommissionTransactions
            .CountAsync(ct => ct.Status == CommissionStatus.Approved && ct.ApprovedBy == adminUserId);
        
        Assert.True(approvedCount >= pendingCommissions.Count * 0.9, // Allow for some failures
            $"Expected at least {pendingCommissions.Count * 0.9} approvals, got {approvedCount}");
        
        _output.WriteLine($"Bulk approved {approvedCount} commissions in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average approval time: {(double)stopwatch.ElapsedMilliseconds / approvedCount:F2}ms per commission");
    }

    [Fact]
    public async Task PaginatedQueries_WithLargeDataset_ShouldMaintainConsistentPerformance()
    {
        // Arrange
        await SetupLargeHierarchyAsync();
        var mlmService = _serviceProvider.GetRequiredService<IMLMService>();
        
        // Get a root user with many children
        var rootUser = await _dbContext.UserReferrals
            .Where(ur => ur.Level == 0)
            .FirstAsync();
        
        const int pageSize = 50;
        const int maxQueryTimeMs = 2000; // 2 seconds per page
        var pageTimes = new List<long>();
        
        // Test multiple pages
        for (int page = 1; page <= 10; page++)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var request = new GetDownlineRequest
            {
                PageNumber = page,
                PageSize = pageSize,
                MaxDepth = 3
            };
            
            var result = await mlmService.GetUserDownlineAsync(rootUser.UserId, request);
            
            stopwatch.Stop();
            pageTimes.Add(stopwatch.ElapsedMilliseconds);
            
            Assert.True(stopwatch.ElapsedMilliseconds < maxQueryTimeMs,
                $"Page {page} query took {stopwatch.ElapsedMilliseconds}ms, expected < {maxQueryTimeMs}ms");
            
            _output.WriteLine($"Page {page}: {stopwatch.ElapsedMilliseconds}ms, {result.Items.Count()} items");
            
            if (result.Items.Count() < pageSize)
            {
                _output.WriteLine($"Reached end of data at page {page}");
                break;
            }
        }
        
        // Assert consistent performance
        var avgTime = pageTimes.Average();
        var maxTime = pageTimes.Max();
        var minTime = pageTimes.Min();
        
        _output.WriteLine($"Pagination performance - Avg: {avgTime:F2}ms, Min: {minTime}ms, Max: {maxTime}ms");
        
        // Performance should not degrade significantly across pages
        Assert.True(maxTime - minTime < avgTime * 2, 
            "Performance degradation across pages is too high");
    }

    [Fact]
    public async Task ConcurrentCommissionProcessing_With100SimultaneousPayments_ShouldHandleCorrectly()
    {
        // Arrange
        await SetupCommissionRulesAsync();
        await SetupMediumHierarchyAsync(); // Smaller hierarchy for concurrency test
        
        var calculationService = _serviceProvider.GetRequiredService<CommissionCalculationService>();
        const int concurrentPayments = 100;
        const int maxProcessingTimeMs = 15000; // 15 seconds
        
        // Get users for testing
        var testUsers = await _dbContext.UserReferrals
            .Where(ur => ur.Level >= 2)
            .Take(concurrentPayments)
            .ToListAsync();
        
        var paymentTasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();
        
        // Create concurrent payment processing tasks
        foreach (var user in testUsers)
        {
            var paymentId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var amount = Random.Shared.Next(100, 2000);
            
            paymentTasks.Add(Task.Run(async () =>
            {
                try
                {
                    await calculationService.ProcessPaymentConfirmedAsync(paymentId, user.UserId, amount, productId);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Error processing payment {paymentId}: {ex.Message}");
                    throw;
                }
            }));
        }
        
        // Wait for all payments to complete
        await Task.WhenAll(paymentTasks);
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < maxProcessingTimeMs,
            $"Concurrent processing took {stopwatch.ElapsedMilliseconds}ms, expected < {maxProcessingTimeMs}ms");
        
        // Verify data integrity
        var totalCommissions = await _dbContext.CommissionTransactions.CountAsync();
        var uniquePayments = await _dbContext.CommissionTransactions
            .Select(ct => ct.SourcePaymentId)
            .Distinct()
            .CountAsync();
        
        Assert.Equal(testUsers.Count, uniquePayments);
        Assert.True(totalCommissions >= testUsers.Count); // Should have at least one commission per payment
        
        _output.WriteLine($"Processed {concurrentPayments} concurrent payments in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Generated {totalCommissions} commission transactions");
        _output.WriteLine($"Average processing time: {(double)stopwatch.ElapsedMilliseconds / concurrentPayments:F2}ms per payment");
    }

    private async Task SetupCommissionRulesAsync()
    {
        var ruleSet = CommissionRuleSet.Create(
            "Performance Test Rules",
            "Commission rules for performance testing"
        );
        
        var schemaJson = """
        {
            "levels": [
                { "level": 1, "percentage": 10 },
                { "level": 2, "percentage": 5 },
                { "level": 3, "percentage": 2 },
                { "level": 4, "percentage": 1 },
                { "level": 5, "percentage": 0.5 }
            ]
        }
        """;
        
        var version = ruleSet.CreateVersion(schemaJson, "Performance test version");
        version.Publish();
        ruleSet.Activate();
        
        _dbContext.CommissionRuleSets.Add(ruleSet);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SetupLargeHierarchyAsync()
    {
        // Create a 6-level hierarchy with branching
        var users = new List<UserReferral>();
        
        // Level 0 - Root
        var rootUser = UserReferral.CreateRoot(Guid.NewGuid());
        users.Add(rootUser);
        
        var currentLevelUsers = new List<UserReferral> { rootUser };
        
        // Create 6 levels with increasing width
        for (int level = 1; level <= 6; level++)
        {
            var nextLevelUsers = new List<UserReferral>();
            var childrenPerParent = Math.Min(level * 2, 10); // Max 10 children per parent
            
            foreach (var parent in currentLevelUsers)
            {
                for (int i = 0; i < childrenPerParent; i++)
                {
                    var child = UserReferral.Create(Guid.NewGuid(), parent);
                    users.Add(child);
                    nextLevelUsers.Add(child);
                }
            }
            
            currentLevelUsers = nextLevelUsers;
            _output.WriteLine($"Created level {level} with {currentLevelUsers.Count} users");
        }
        
        // Add users in batches to avoid memory issues
        var batchSize = 1000;
        for (int i = 0; i < users.Count; i += batchSize)
        {
            var batch = users.Skip(i).Take(batchSize);
            _dbContext.UserReferrals.AddRange(batch);
            await _dbContext.SaveChangesAsync();
        }
        
        _output.WriteLine($"Created large hierarchy with {users.Count} total users");
    }

    private async Task SetupMediumHierarchyAsync()
    {
        // Create a smaller hierarchy for concurrency testing
        var users = new List<UserReferral>();
        
        // Level 0 - Root
        var rootUser = UserReferral.CreateRoot(Guid.NewGuid());
        users.Add(rootUser);
        
        var currentLevelUsers = new List<UserReferral> { rootUser };
        
        // Create 4 levels
        for (int level = 1; level <= 4; level++)
        {
            var nextLevelUsers = new List<UserReferral>();
            var childrenPerParent = 5; // 5 children per parent
            
            foreach (var parent in currentLevelUsers)
            {
                for (int i = 0; i < childrenPerParent; i++)
                {
                    var child = UserReferral.Create(Guid.NewGuid(), parent);
                    users.Add(child);
                    nextLevelUsers.Add(child);
                }
            }
            
            currentLevelUsers = nextLevelUsers;
        }
        
        _dbContext.UserReferrals.AddRange(users);
        await _dbContext.SaveChangesAsync();
        
        _output.WriteLine($"Created medium hierarchy with {users.Count} total users");
    }

    private async Task CreateBulkCommissionsAsync(int count)
    {
        // Create some users first
        var users = new List<UserReferral>();
        for (int i = 0; i < Math.Min(count / 10, 100); i++)
        {
            users.Add(UserReferral.CreateRoot(Guid.NewGuid()));
        }
        
        _dbContext.UserReferrals.AddRange(users);
        await _dbContext.SaveChangesAsync();
        
        // Create commission transactions
        var commissions = new List<CommissionTransaction>();
        for (int i = 0; i < count; i++)
        {
            var user = users[i % users.Count];
            var commission = CommissionTransaction.Create(
                user.UserId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Random.Shared.Next(100, 2000),
                Random.Shared.Next(1, 4),
                Random.Shared.Next(10, 200)
            );
            commissions.Add(commission);
        }
        
        // Add in batches
        var batchSize = 100;
        for (int i = 0; i < commissions.Count; i += batchSize)
        {
            var batch = commissions.Skip(i).Take(batchSize);
            _dbContext.CommissionTransactions.AddRange(batch);
            await _dbContext.SaveChangesAsync();
        }
        
        _output.WriteLine($"Created {commissions.Count} commission transactions for testing");
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