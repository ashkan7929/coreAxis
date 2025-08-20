using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.ValueObjects;
using Xunit;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class UserReferralTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateUserReferral()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var parentUserId = Guid.NewGuid();
        var path = "1.2";
        var status = ReferralStatus.Active;
        var joinedAt = DateTime.UtcNow;

        // Act
        var userReferral = UserReferral.Create(userId, parentUserId, path, status, joinedAt);

        // Assert
        Assert.NotNull(userReferral);
        Assert.Equal(userId, userReferral.UserId);
        Assert.Equal(parentUserId, userReferral.ParentUserId);
        Assert.Equal(path, userReferral.Path);
        Assert.Equal(status, userReferral.Status);
        Assert.Equal(joinedAt, userReferral.JoinedAt);
    }

    [Fact]
    public void Create_WithNullParent_ShouldCreateRootUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var path = "1";
        var status = ReferralStatus.Active;
        var joinedAt = DateTime.UtcNow;

        // Act
        var userReferral = UserReferral.Create(userId, null, path, status, joinedAt);

        // Assert
        Assert.NotNull(userReferral);
        Assert.Equal(userId, userReferral.UserId);
        Assert.Null(userReferral.ParentUserId);
        Assert.Equal(path, userReferral.Path);
        Assert.Equal(status, userReferral.Status);
        Assert.Equal(joinedAt, userReferral.JoinedAt);
    }

    [Fact]
    public void Activate_WhenInactive_ShouldChangeStatusToActive()
    {
        // Arrange
        var userReferral = UserReferral.Create(
            Guid.NewGuid(),
            null,
            "1",
            ReferralStatus.Inactive,
            DateTime.UtcNow
        );

        // Act
        userReferral.Activate();

        // Assert
        Assert.Equal(ReferralStatus.Active, userReferral.Status);
        Assert.NotNull(userReferral.ActivatedAt);
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldChangeStatusToInactive()
    {
        // Arrange
        var userReferral = UserReferral.Create(
            Guid.NewGuid(),
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow
        );

        // Act
        userReferral.Deactivate();

        // Assert
        Assert.Equal(ReferralStatus.Inactive, userReferral.Status);
        Assert.NotNull(userReferral.DeactivatedAt);
    }

    [Fact]
    public void UpdatePath_WithValidPath_ShouldUpdatePath()
    {
        // Arrange
        var userReferral = UserReferral.Create(
            Guid.NewGuid(),
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow
        );
        var newPath = "1.2.3";

        // Act
        userReferral.UpdatePath(newPath);

        // Assert
        Assert.Equal(newPath, userReferral.Path);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void UpdatePath_WithInvalidPath_ShouldThrowException(string invalidPath)
    {
        // Arrange
        var userReferral = UserReferral.Create(
            Guid.NewGuid(),
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => userReferral.UpdatePath(invalidPath));
    }

    [Fact]
    public void GetLevel_WithRootPath_ShouldReturnOne()
    {
        // Arrange
        var userReferral = UserReferral.Create(
            Guid.NewGuid(),
            null,
            "1",
            ReferralStatus.Active,
            DateTime.UtcNow
        );

        // Act
        var level = userReferral.GetLevel();

        // Assert
        Assert.Equal(1, level);
    }

    [Fact]
    public void GetLevel_WithDeepPath_ShouldReturnCorrectLevel()
    {
        // Arrange
        var userReferral = UserReferral.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1.2.3.4",
            ReferralStatus.Active,
            DateTime.UtcNow
        );

        // Act
        var level = userReferral.GetLevel();

        // Assert
        Assert.Equal(4, level);
    }

    [Fact]
    public void IsDescendantOf_WithParentPath_ShouldReturnTrue()
    {
        // Arrange
        var userReferral = UserReferral.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1.2.3",
            ReferralStatus.Active,
            DateTime.UtcNow
        );
        var parentPath = "1.2";

        // Act
        var isDescendant = userReferral.IsDescendantOf(parentPath);

        // Assert
        Assert.True(isDescendant);
    }

    [Fact]
    public void IsDescendantOf_WithNonParentPath_ShouldReturnFalse()
    {
        // Arrange
        var userReferral = UserReferral.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1.2.3",
            ReferralStatus.Active,
            DateTime.UtcNow
        );
        var nonParentPath = "1.4";

        // Act
        var isDescendant = userReferral.IsDescendantOf(nonParentPath);

        // Assert
        Assert.False(isDescendant);
    }

    [Fact]
    public void IsDescendantOf_WithSamePath_ShouldReturnFalse()
    {
        // Arrange
        var userReferral = UserReferral.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "1.2.3",
            ReferralStatus.Active,
            DateTime.UtcNow
        );
        var samePath = "1.2.3";

        // Act
        var isDescendant = userReferral.IsDescendantOf(samePath);

        // Assert
        Assert.False(isDescendant);
    }
}