using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.ValueObjects;
using Xunit;

namespace CoreAxis.Tests.MLMModule.UnitTests;

public class CommissionRuleSetTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCommissionRuleSet()
    {
        // Arrange
        var name = "Test RuleSet";
        var description = "Test description";
        var isActive = true;

        // Act
        var ruleSet = CommissionRuleSet.Create(name, description, isActive);

        // Assert
        Assert.NotNull(ruleSet);
        Assert.Equal(name, ruleSet.Name);
        Assert.Equal(description, ruleSet.Description);
        Assert.Equal(isActive, ruleSet.IsActive);
        Assert.NotEqual(Guid.Empty, ruleSet.Id);
        Assert.True(ruleSet.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            CommissionRuleSet.Create(invalidName, "Description", true));
    }

    [Fact]
    public void CreateVersion_WithValidData_ShouldCreateVersion()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test RuleSet", "Description", true);
        var schemaJson = "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}";
        var createdBy = Guid.NewGuid();

        // Act
        var version = ruleSet.CreateVersion(schemaJson, createdBy);

        // Assert
        Assert.NotNull(version);
        Assert.Equal(ruleSet.Id, version.RuleSetId);
        Assert.Equal(schemaJson, version.SchemaJson);
        Assert.Equal(createdBy, version.CreatedBy);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(RuleVersionStatus.Draft, version.Status);
        Assert.Contains(version, ruleSet.Versions);
    }

    [Fact]
    public void CreateVersion_MultipleVersions_ShouldIncrementVersionNumber()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test RuleSet", "Description", true);
        var schemaJson1 = "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}";
        var schemaJson2 = "{\"rules\": [{\"level\": 1, \"percentage\": 15}]}";
        var createdBy = Guid.NewGuid();

        // Act
        var version1 = ruleSet.CreateVersion(schemaJson1, createdBy);
        var version2 = ruleSet.CreateVersion(schemaJson2, createdBy);

        // Assert
        Assert.Equal(1, version1.VersionNumber);
        Assert.Equal(2, version2.VersionNumber);
        Assert.Equal(2, ruleSet.Versions.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void CreateVersion_WithInvalidSchemaJson_ShouldThrowException(string invalidSchema)
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test RuleSet", "Description", true);
        var createdBy = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            ruleSet.CreateVersion(invalidSchema, createdBy));
    }

    [Fact]
    public void Activate_WhenInactive_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test RuleSet", "Description", false);

        // Act
        ruleSet.Activate();

        // Assert
        Assert.True(ruleSet.IsActive);
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test RuleSet", "Description", true);

        // Act
        ruleSet.Deactivate();

        // Assert
        Assert.False(ruleSet.IsActive);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Old Name", "Old Description", true);
        var newName = "New Name";
        var newDescription = "New Description";

        // Act
        ruleSet.Update(newName, newDescription);

        // Assert
        Assert.Equal(newName, ruleSet.Name);
        Assert.Equal(newDescription, ruleSet.Description);
        Assert.True(ruleSet.UpdatedAt.HasValue);
        Assert.True(ruleSet.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void GetActiveVersion_WithPublishedVersion_ShouldReturnPublishedVersion()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test RuleSet", "Description", true);
        var schemaJson = "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}";
        var createdBy = Guid.NewGuid();
        
        var version = ruleSet.CreateVersion(schemaJson, createdBy);
        version.Publish(createdBy);

        // Act
        var activeVersion = ruleSet.GetActiveVersion();

        // Assert
        Assert.NotNull(activeVersion);
        Assert.Equal(version.Id, activeVersion.Id);
        Assert.Equal(RuleVersionStatus.Published, activeVersion.Status);
    }

    [Fact]
    public void GetActiveVersion_WithoutPublishedVersion_ShouldReturnNull()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test RuleSet", "Description", true);
        var schemaJson = "{\"rules\": [{\"level\": 1, \"percentage\": 10}]}";
        var createdBy = Guid.NewGuid();
        
        ruleSet.CreateVersion(schemaJson, createdBy); // Draft version

        // Act
        var activeVersion = ruleSet.GetActiveVersion();

        // Assert
        Assert.Null(activeVersion);
    }

    [Fact]
    public void GetLatestVersion_WithMultipleVersions_ShouldReturnLatestVersion()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test RuleSet", "Description", true);
        var createdBy = Guid.NewGuid();
        
        var version1 = ruleSet.CreateVersion("{\"rules\": [{\"level\": 1, \"percentage\": 10}]}", createdBy);
        var version2 = ruleSet.CreateVersion("{\"rules\": [{\"level\": 1, \"percentage\": 15}]}", createdBy);
        var version3 = ruleSet.CreateVersion("{\"rules\": [{\"level\": 1, \"percentage\": 20}]}", createdBy);

        // Act
        var latestVersion = ruleSet.GetLatestVersion();

        // Assert
        Assert.NotNull(latestVersion);
        Assert.Equal(version3.Id, latestVersion.Id);
        Assert.Equal(3, latestVersion.VersionNumber);
    }

    [Fact]
    public void GetLatestVersion_WithoutVersions_ShouldReturnNull()
    {
        // Arrange
        var ruleSet = CommissionRuleSet.Create("Test RuleSet", "Description", true);

        // Act
        var latestVersion = ruleSet.GetLatestVersion();

        // Assert
        Assert.Null(latestVersion);
    }
}