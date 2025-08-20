using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.Modules.MLMModule.Domain.ValueObjects;
using CoreAxis.Modules.MLMModule.Infrastructure.Data;
using CoreAxis.Modules.MLMModule.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class UserReferralRepositoryTests : IDisposable
{
    private readonly DbContextOptions<MLMModuleDbContext> _options;
    private readonly MLMModuleDbContext _context;
    private readonly UserReferralRepository _repository;

    public UserReferralRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<MLMModuleDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new MLMModuleDbContext(_options);
        _repository = new UserReferralRepository(_context);
    }

    [Fact]
    public async Task GetUplineUsersAsync_WithValidPath_ShouldReturnUplineUsers()
    {
        // Arrange
        var rootUserId = Guid.NewGuid();
        var level2UserId = Guid.NewGuid();
        var level3UserId = Guid.NewGuid();
        var level4UserId = Guid.NewGuid();
        
        var rootUser = UserReferral.Create(
            rootUserId,
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-100)
        );
        
        var level2User = UserReferral.Create(
            level2UserId,
            rootUserId,
            "1.2",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-50)
        );
        
        var level3User = UserReferral.Create(
            level3UserId,
            level2UserId,
            "1.2.3",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-25)
        );
        
        var level4User = UserReferral.Create(
            level4UserId,
            level3UserId,
            "1.2.3.4",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-1)
        );
        
        await _repository.AddAsync(rootUser);
        await _repository.AddAsync(level2User);
        await _repository.AddAsync(level3User);
        await _repository.AddAsync(level4User);
        await _context.SaveChangesAsync();

        // Act
        var uplineUsers = await _repository.GetUplineUsersAsync(level4UserId);

        // Assert
        Assert.Equal(3, uplineUsers.Count);
        
        // Should be ordered by level (closest parent first)
        Assert.Equal(level3UserId, uplineUsers[0].UserId); // Direct parent
        Assert.Equal(level2UserId, uplineUsers[1].UserId); // Grandparent
        Assert.Equal(rootUserId, uplineUsers[2].UserId);   // Great-grandparent
    }

    [Fact]
    public async Task GetDownlineUsersAsync_WithValidPath_ShouldReturnDownlineUsers()
    {
        // Arrange
        var rootUserId = Guid.NewGuid();
        var level2UserId = Guid.NewGuid();
        var level3UserId = Guid.NewGuid();
        var level4UserId = Guid.NewGuid();
        
        var rootUser = UserReferral.Create(
            rootUserId,
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-100)
        );
        
        var level2User = UserReferral.Create(
            level2UserId,
            rootUserId,
            "1.2",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-50)
        );
        
        var level3User = UserReferral.Create(
            level3UserId,
            level2UserId,
            "1.2.3",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-25)
        );
        
        var level4User = UserReferral.Create(
            level4UserId,
            level3UserId,
            "1.2.3.4",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-1)
        );
        
        await _repository.AddAsync(rootUser);
        await _repository.AddAsync(level2User);
        await _repository.AddAsync(level3User);
        await _repository.AddAsync(level4User);
        await _context.SaveChangesAsync();

        // Act
        var downlineUsers = await _repository.GetDownlineUsersAsync(rootUserId);

        // Assert
        Assert.Equal(3, downlineUsers.Count);
        Assert.Contains(downlineUsers, u => u.UserId == level2UserId);
        Assert.Contains(downlineUsers, u => u.UserId == level3UserId);
        Assert.Contains(downlineUsers, u => u.UserId == level4UserId);
    }

    [Fact]
    public async Task GetDirectChildrenAsync_WithValidUserId_ShouldReturnDirectChildren()
    {
        // Arrange
        var parentUserId = Guid.NewGuid();
        var child1UserId = Guid.NewGuid();
        var child2UserId = Guid.NewGuid();
        var grandchildUserId = Guid.NewGuid();
        
        var parentUser = UserReferral.Create(
            parentUserId,
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-100)
        );
        
        var child1User = UserReferral.Create(
            child1UserId,
            parentUserId,
            "1.2",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-50)
        );
        
        var child2User = UserReferral.Create(
            child2UserId,
            parentUserId,
            "1.3",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-40)
        );
        
        var grandchildUser = UserReferral.Create(
            grandchildUserId,
            child1UserId,
            "1.2.4",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-1)
        );
        
        await _repository.AddAsync(parentUser);
        await _repository.AddAsync(child1User);
        await _repository.AddAsync(child2User);
        await _repository.AddAsync(grandchildUser);
        await _context.SaveChangesAsync();

        // Act
        var directChildren = await _repository.GetDirectChildrenAsync(parentUserId);

        // Assert
        Assert.Equal(2, directChildren.Count);
        Assert.Contains(directChildren, u => u.UserId == child1UserId);
        Assert.Contains(directChildren, u => u.UserId == child2UserId);
        Assert.DoesNotContain(directChildren, u => u.UserId == grandchildUserId);
    }

    [Fact]
    public async Task GetNetworkStatsAsync_WithValidUserId_ShouldReturnCorrectStats()
    {
        // Arrange
        var rootUserId = Guid.NewGuid();
        var level2User1Id = Guid.NewGuid();
        var level2User2Id = Guid.NewGuid();
        var level3UserId = Guid.NewGuid();
        
        var rootUser = UserReferral.Create(
            rootUserId,
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-100)
        );
        
        var level2User1 = UserReferral.Create(
            level2User1Id,
            rootUserId,
            "1.2",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-50)
        );
        
        var level2User2 = UserReferral.Create(
            level2User2Id,
            rootUserId,
            "1.3",
            ReferralStatus.Inactive, // Inactive user
            DateTime.UtcNow.AddDays(-40)
        );
        
        var level3User = UserReferral.Create(
            level3UserId,
            level2User1Id,
            "1.2.4",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-1)
        );
        
        await _repository.AddAsync(rootUser);
        await _repository.AddAsync(level2User1);
        await _repository.AddAsync(level2User2);
        await _repository.AddAsync(level3User);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _repository.GetNetworkStatsAsync(rootUserId);

        // Assert
        Assert.Equal(3, stats.TotalDownlineCount); // All downline users
        Assert.Equal(2, stats.ActiveDownlineCount); // Only active users
        Assert.Equal(2, stats.DirectReferralsCount); // Direct children
        Assert.Equal(3, stats.MaxDepth); // Root -> Level2 -> Level3
    }

    [Fact]
    public async Task GetByUserIdAsync_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userReferral = UserReferral.Create(
            userId,
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow
        );
        
        await _repository.AddAsync(userReferral);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        var nonExistingUserId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByUserIdAsync(nonExistingUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUsersAtLevelAsync_WithValidLevel_ShouldReturnUsersAtThatLevel()
    {
        // Arrange
        var rootUserId = Guid.NewGuid();
        var level2User1Id = Guid.NewGuid();
        var level2User2Id = Guid.NewGuid();
        var level3UserId = Guid.NewGuid();
        
        var rootUser = UserReferral.Create(
            rootUserId,
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-100)
        );
        
        var level2User1 = UserReferral.Create(
            level2User1Id,
            rootUserId,
            "1.2",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-50)
        );
        
        var level2User2 = UserReferral.Create(
            level2User2Id,
            rootUserId,
            "1.3",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-40)
        );
        
        var level3User = UserReferral.Create(
            level3UserId,
            level2User1Id,
            "1.2.4",
            ReferralStatus.Active,
            DateTime.UtcNow.AddDays(-1)
        );
        
        await _repository.AddAsync(rootUser);
        await _repository.AddAsync(level2User1);
        await _repository.AddAsync(level2User2);
        await _repository.AddAsync(level3User);
        await _context.SaveChangesAsync();

        // Act
        var level2Users = await _repository.GetUsersAtLevelAsync(2);

        // Assert
        Assert.Equal(2, level2Users.Count);
        Assert.Contains(level2Users, u => u.UserId == level2User1Id);
        Assert.Contains(level2Users, u => u.UserId == level2User2Id);
        Assert.DoesNotContain(level2Users, u => u.UserId == rootUserId);
        Assert.DoesNotContain(level2Users, u => u.UserId == level3UserId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}