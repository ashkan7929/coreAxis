using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using CoreAxis.SharedKernel;

namespace CoreAxis.Tests.Modules.DynamicForm.Application;

public class FormDefinitionTests
{
    private DynamicFormDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DynamicFormDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new DynamicFormDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void Form_Create_ShouldCreateValidForm()
    {
        // Arrange
        var name = "Test Form";
        var description = "Test Description";
        var schemaJson = "{\"fields\":[{\"name\":\"field1\",\"type\":\"text\"}]}";
        var tenantId = "tenant1";
        var createdBy = "user1";

        // Act
        var form = new Form(name, schemaJson, tenantId, createdBy)
        {
            Description = description
        };

        // Assert
        form.Should().NotBeNull();
        form.Name.Should().Be(name);
        form.Description.Should().Be(description);
        form.Schema.Should().Be(schemaJson);
        form.TenantId.Should().Be(tenantId);
        form.IsPublished.Should().BeFalse();
        form.Version.Should().Be(1);
        form.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Form_Update_ShouldUpdateFormProperties()
    {
        // Arrange
        var form = new Form("Test Form", "{\"fields\":[]}", "tenant1", "user1");
        var newName = "Updated Form";
        var newDescription = "Updated Description";
        var newSchema = "{\"fields\":[{\"name\":\"field1\",\"type\":\"text\"}]}";
        var modifiedBy = "user2";

        // Act
        form.Update(newName, newDescription, newSchema, modifiedBy);

        // Assert
        form.Name.Should().Be(newName);
        form.Description.Should().Be(newDescription);
        form.Schema.Should().Be(newSchema);
        form.LastModifiedBy.Should().Be(modifiedBy);
    }

    [Fact]
    public void Form_Publish_ShouldSetIsPublishedToTrue()
    {
        // Arrange
        var form = new Form("Test Form", "{\"fields\":[]}", "tenant1", "user1");
        var publishedBy = "user2";

        // Act
        form.Publish(publishedBy);

        // Assert
        form.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Form_Unpublish_ShouldSetIsPublishedToFalse()
    {
        // Arrange
        var form = new Form("Test Form", "{\"fields\":[]}", "tenant1", "user1");
        form.Publish("user1");
        var unpublishedBy = "user2";

        // Act
        form.Unpublish(unpublishedBy);

        // Assert
        form.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task FormRepository_AddAsync_ShouldSaveFormToDatabase()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var form = new Form("Test Form", "{\"fields\":[]}", "tenant1", "user1");

        // Act
        context.Forms.Add(form);
        await context.SaveChangesAsync();

        // Assert
        var savedForm = await context.Forms.FirstOrDefaultAsync(f => f.Id == form.Id);
        savedForm.Should().NotBeNull();
        savedForm!.Name.Should().Be("Test Form");
    }
}