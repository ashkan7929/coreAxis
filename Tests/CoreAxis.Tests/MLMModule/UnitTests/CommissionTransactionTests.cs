using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.ValueObjects;
using Xunit;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class CommissionTransactionTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCommissionTransaction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sourcePaymentId = Guid.NewGuid();
        var amount = 100m;
        var level = 1;
        var ruleSetId = Guid.NewGuid();
        var ruleVersionId = Guid.NewGuid();
        var notes = "Test commission";

        // Act
        var transaction = CommissionTransaction.Create(
            userId,
            sourcePaymentId,
            amount,
            level,
            ruleSetId,
            ruleVersionId,
            notes
        );

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal(userId, transaction.UserId);
        Assert.Equal(sourcePaymentId, transaction.SourcePaymentId);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal(level, transaction.Level);
        Assert.Equal(ruleSetId, transaction.RuleSetId);
        Assert.Equal(ruleVersionId, transaction.RuleVersionId);
        Assert.Equal(notes, transaction.Notes);
        Assert.Equal(CommissionStatus.Pending, transaction.Status);
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.True(transaction.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidAmount_ShouldThrowException(decimal invalidAmount)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sourcePaymentId = Guid.NewGuid();
        var level = 1;
        var ruleSetId = Guid.NewGuid();
        var ruleVersionId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            CommissionTransaction.Create(
                userId,
                sourcePaymentId,
                invalidAmount,
                level,
                ruleSetId,
                ruleVersionId,
                "Test"
            ));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidLevel_ShouldThrowException(int invalidLevel)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sourcePaymentId = Guid.NewGuid();
        var amount = 100m;
        var ruleSetId = Guid.NewGuid();
        var ruleVersionId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            CommissionTransaction.Create(
                userId,
                sourcePaymentId,
                amount,
                invalidLevel,
                ruleSetId,
                ruleVersionId,
                "Test"
            ));
    }

    [Fact]
    public void Approve_WhenPending_ShouldChangeStatusToApproved()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );
        var approvedBy = Guid.NewGuid();

        // Act
        transaction.Approve(approvedBy);

        // Assert
        Assert.Equal(CommissionStatus.Approved, transaction.Status);
        Assert.Equal(approvedBy, transaction.ApprovedBy);
        Assert.NotNull(transaction.ApprovedAt);
        Assert.True(transaction.ApprovedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldThrowException()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );
        var approvedBy = Guid.NewGuid();
        transaction.Approve(approvedBy);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transaction.Approve(approvedBy));
    }

    [Fact]
    public void Reject_WhenPending_ShouldChangeStatusToRejected()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );
        var rejectedBy = Guid.NewGuid();
        var reason = "Invalid transaction";

        // Act
        transaction.Reject(rejectedBy, reason);

        // Assert
        Assert.Equal(CommissionStatus.Rejected, transaction.Status);
        Assert.Equal(rejectedBy, transaction.RejectedBy);
        Assert.Equal(reason, transaction.RejectionReason);
        Assert.NotNull(transaction.RejectedAt);
        Assert.True(transaction.RejectedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Reject_WhenAlreadyApproved_ShouldThrowException()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );
        var approvedBy = Guid.NewGuid();
        var rejectedBy = Guid.NewGuid();
        transaction.Approve(approvedBy);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            transaction.Reject(rejectedBy, "Reason"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Reject_WithInvalidReason_ShouldThrowException(string invalidReason)
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );
        var rejectedBy = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            transaction.Reject(rejectedBy, invalidReason));
    }

    [Fact]
    public void Expire_WhenPending_ShouldChangeStatusToExpired()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );

        // Act
        transaction.Expire();

        // Assert
        Assert.Equal(CommissionStatus.Expired, transaction.Status);
        Assert.NotNull(transaction.ExpiredAt);
        Assert.True(transaction.ExpiredAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Expire_WhenAlreadyApproved_ShouldThrowException()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );
        var approvedBy = Guid.NewGuid();
        transaction.Approve(approvedBy);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transaction.Expire());
    }

    [Fact]
    public void UpdateNotes_WithValidNotes_ShouldUpdateNotes()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Original notes"
        );
        var newNotes = "Updated notes";

        // Act
        transaction.UpdateNotes(newNotes);

        // Assert
        Assert.Equal(newNotes, transaction.Notes);
        Assert.NotNull(transaction.UpdatedAt);
        Assert.True(transaction.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void UpdateNotes_WithNullNotes_ShouldSetNotesToNull()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Original notes"
        );

        // Act
        transaction.UpdateNotes(null);

        // Assert
        Assert.Null(transaction.Notes);
        Assert.NotNull(transaction.UpdatedAt);
    }

    [Fact]
    public void IsPending_WhenStatusIsPending_ShouldReturnTrue()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );

        // Act
        var isPending = transaction.IsPending();

        // Assert
        Assert.True(isPending);
    }

    [Fact]
    public void IsPending_WhenStatusIsApproved_ShouldReturnFalse()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );
        transaction.Approve(Guid.NewGuid());

        // Act
        var isPending = transaction.IsPending();

        // Assert
        Assert.False(isPending);
    }

    [Fact]
    public void IsApproved_WhenStatusIsApproved_ShouldReturnTrue()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );
        transaction.Approve(Guid.NewGuid());

        // Act
        var isApproved = transaction.IsApproved();

        // Assert
        Assert.True(isApproved);
    }

    [Fact]
    public void IsApproved_WhenStatusIsPending_ShouldReturnFalse()
    {
        // Arrange
        var transaction = CommissionTransaction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test"
        );

        // Act
        var isApproved = transaction.IsApproved();

        // Assert
        Assert.False(isApproved);
    }
}