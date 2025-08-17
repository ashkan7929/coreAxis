using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace CoreAxis.Tests.Modules.DynamicForm.Integration;

public class FormsApiTests
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
    public async Task CreateForm_ShouldCreateFormInDatabase()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var form = new CoreAxis.Modules.DynamicForm.Domain.Entities.Form(
            "Test Form", 
            "{\"fields\":[{\"name\":\"field1\",\"type\":\"text\"}]}",
            "tenant1",
            "user1")
        {
            Description = "Test Description"
        };

        // Act
        context.Forms.Add(form);
        await context.SaveChangesAsync();

        // Assert
        var savedForm = await context.Forms.FirstOrDefaultAsync(f => f.Name == "Test Form");
        savedForm.Should().NotBeNull();
        savedForm!.Name.Should().Be("Test Form");
        savedForm.Description.Should().Be("Test Description");
    }

    [Fact]
    public async Task GetForms_ShouldReturnFormsFromDatabase()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var form1 = new CoreAxis.Modules.DynamicForm.Domain.Entities.Form(
            "Test Form 1", 
            "{\"fields\":[{\"name\":\"field1\",\"type\":\"text\"}]}",
            "tenant1",
            "user1")
        {
            Description = "Description 1"
        };
        var form2 = new CoreAxis.Modules.DynamicForm.Domain.Entities.Form(
            "Test Form 2", 
            "{\"fields\":[{\"name\":\"field2\",\"type\":\"text\"}]}",
            "tenant1",
            "user1")
        {
            Description = "Description 2"
        };

        context.Forms.AddRange(form1, form2);
        await context.SaveChangesAsync();

        // Act
        var forms = await context.Forms.ToListAsync();

        // Assert
        forms.Should().NotBeNull();
        forms.Should().HaveCount(2);
        forms.Should().Contain(f => f.Name == "Test Form 1");
        forms.Should().Contain(f => f.Name == "Test Form 2");
    }

    [Fact]
    public async Task FormSubmission_ShouldBeStoredInDatabase()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var form = new CoreAxis.Modules.DynamicForm.Domain.Entities.Form(
            "Test Form", 
            "{\"fields\":[{\"name\":\"field1\",\"type\":\"text\",\"required\":true}]}",
            "tenant1",
            "user1")
        {
            Description = "Test Description"
        };
        
        context.Forms.Add(form);
        await context.SaveChangesAsync();

        var submission = new CoreAxis.Modules.DynamicForm.Domain.Entities.FormSubmission(
             form.Id,
             Guid.NewGuid(), // userId
             "tenant1",
             "{\"field1\":\"test value\"}");

        // Act
        context.FormSubmissions.Add(submission);
        await context.SaveChangesAsync();

        // Assert
        var savedSubmission = await context.FormSubmissions.FirstOrDefaultAsync(s => s.FormId == form.Id);
        savedSubmission.Should().NotBeNull();
        savedSubmission!.SubmissionData.Should().Contain("test value");
        savedSubmission.SubmittedBy.Should().Be("test-user");
    }
}