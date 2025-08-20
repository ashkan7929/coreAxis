using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.ValueObjects;
using Xunit;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class CommissionRuleVersionTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCommissionRuleVersion()
    {
        // Arrange
        var ruleSetId = Guid.NewGuid();
        var versionNumber = 1;
        var schemaJson = "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}";
        var createdBy = Guid.NewGuid();

        // Act
        var version = CommissionRuleVersion.Create(ruleSetId, versionNumber, schemaJson, createdBy);

        // Assert
        Assert.NotNull(version);
        Assert.Equal(ruleSetId, version.RuleSetId);
        Assert.Equal(versionNumber, version.VersionNumber);
        Assert.Equal(schemaJson, version.SchemaJson);
        Assert.Equal(createdBy, version.CreatedBy);
        Assert.Equal(RuleVersionStatus.Draft, version.Status);
        Assert.NotEqual(Guid.Empty, version.Id);
        Assert.True(version.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_WithInvalidSchemaJson_ShouldThrowException(string invalidSchema)
    {
        // Arrange
        var ruleSetId = Guid.NewGuid();
        var versionNumber = 1;
        var createdBy = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            CommissionRuleVersion.Create(ruleSetId, versionNumber, invalidSchema, createdBy));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidVersionNumber_ShouldThrowException(int invalidVersionNumber)
    {
        // Arrange
        var ruleSetId = Guid.NewGuid();
        var schemaJson = "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}";
        var createdBy = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            CommissionRuleVersion.Create(ruleSetId, invalidVersionNumber, schemaJson, createdBy));
    }

    [Fact]
    public void Publish_WhenDraft_ShouldChangeStatusToPublished()
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );
        var publishedBy = Guid.NewGuid();

        // Act
        version.Publish(publishedBy);

        // Assert
        Assert.Equal(RuleVersionStatus.Published, version.Status);
        Assert.Equal(publishedBy, version.PublishedBy);
        Assert.NotNull(version.PublishedAt);
        Assert.True(version.PublishedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ShouldThrowException()
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );
        var publishedBy = Guid.NewGuid();
        version.Publish(publishedBy);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => version.Publish(publishedBy));
    }

    [Fact]
    public void Archive_WhenPublished_ShouldChangeStatusToArchived()
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );
        var publishedBy = Guid.NewGuid();
        var archivedBy = Guid.NewGuid();
        
        version.Publish(publishedBy);

        // Act
        version.Archive(archivedBy);

        // Assert
        Assert.Equal(RuleVersionStatus.Archived, version.Status);
        Assert.Equal(archivedBy, version.ArchivedBy);
        Assert.NotNull(version.ArchivedAt);
        Assert.True(version.ArchivedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Archive_WhenDraft_ShouldThrowException()
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );
        var archivedBy = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => version.Archive(archivedBy));
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldThrowException()
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );
        var publishedBy = Guid.NewGuid();
        var archivedBy = Guid.NewGuid();
        
        version.Publish(publishedBy);
        version.Archive(archivedBy);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => version.Archive(archivedBy));
    }

    [Fact]
    public void UpdateSchema_WhenDraft_ShouldUpdateSchemaJson()
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );
        var newSchemaJson = "{\"rules\": [{\"level\": 1, \"percentage\": 15}]}";

        // Act
        version.UpdateSchema(newSchemaJson);

        // Assert
        Assert.Equal(newSchemaJson, version.SchemaJson);
        Assert.NotNull(version.UpdatedAt);
        Assert.True(version.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void UpdateSchema_WhenPublished_ShouldThrowException()
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );
        var publishedBy = Guid.NewGuid();
        version.Publish(publishedBy);
        
        var newSchemaJson = "{\"rules\": [{\"level\": 1, \"percentage\": 15}]}";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => version.UpdateSchema(newSchemaJson));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void UpdateSchema_WithInvalidSchemaJson_ShouldThrowException(string invalidSchema)
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => version.UpdateSchema(invalidSchema));
    }

    [Fact]
    public void IsEditable_WhenDraft_ShouldReturnTrue()
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );

        // Act
        var isEditable = version.IsEditable();

        // Assert
        Assert.True(isEditable);
    }

    [Fact]
    public void IsEditable_WhenPublished_ShouldReturnFalse()
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );
        var publishedBy = Guid.NewGuid();
        version.Publish(publishedBy);

        // Act
        var isEditable = version.IsEditable();

        // Assert
        Assert.False(isEditable);
    }

    [Fact]
    public void IsEditable_WhenArchived_ShouldReturnFalse()
    {
        // Arrange
        var version = CommissionRuleVersion.Create(
            Guid.NewGuid(),
            1,
            "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}",
            Guid.NewGuid()
        );
        var publishedBy = Guid.NewGuid();
        var archivedBy = Guid.NewGuid();
        
        version.Publish(publishedBy);
        version.Archive(archivedBy);

        // Act
        var isEditable = version.IsEditable();

        // Assert
        Assert.False(isEditable);
    }
}