using CoreAxis.Modules.ApiManager.Application.Services;
using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.Modules.DynamicForm.Infrastructure.Persistence;
using CoreAxis.SharedKernel.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.Modules.DynamicForm.Integration;

/// <summary>
/// Integration tests for DynamicOptionsManager
/// </summary>
public class DynamicOptionsManagerIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly DynamicFormDbContext _dbContext;
    private readonly IDynamicOptionsManager _dynamicOptionsManager;

    public DynamicOptionsManagerIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add DbContext with InMemory database
        services.AddDbContext<DynamicFormDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add memory cache
        services.AddMemoryCache();
        
        // Add mocked dependencies
        var mockApiManager = new Mock<IApiManager>();
        services.AddSingleton(mockApiManager.Object);
        
        // Add ExpressionEngine
        services.AddScoped<IExpressionEngine, ExpressionEngine>();
        
        // Add DynamicOptionsManager
        services.AddScoped<IDynamicOptionsManager, DynamicOptionsManager>();
        
        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<DynamicFormDbContext>();
        _dynamicOptionsManager = _serviceProvider.GetRequiredService<IDynamicOptionsManager>();
        
        // Ensure database is created
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task EvaluateDynamicOptionsAsync_WithStaticExpression_ShouldReturnOptions()
    {
        // Arrange
        var expression = "static([{\"value\": \"1\", \"label\": \"Option 1\"}, {\"value\": \"2\", \"label\": \"Option 2\"}])";
        var formData = new Dictionary<string, object?> { ["field1"] = "value1" };

        // Act
        var result = await _dynamicOptionsManager.EvaluateDynamicOptionsAsync(expression, formData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("1", result.Value[0].Value);
        Assert.Equal("Option 1", result.Value[0].Label);
        Assert.Equal("2", result.Value[1].Value);
        Assert.Equal("Option 2", result.Value[1].Label);
    }

    [Fact]
    public async Task EvaluateDynamicOptionsAsync_WithConditionalExpression_ShouldReturnFilteredOptions()
    {
        // Arrange
        var expression = "if(field1 == 'show_all', static([{\"value\": \"1\", \"label\": \"Option 1\"}, {\"value\": \"2\", \"label\": \"Option 2\"}]), static([{\"value\": \"1\", \"label\": \"Option 1\"}]))";
        var formData = new Dictionary<string, object?> { ["field1"] = "show_all" };

        // Act
        var result = await _dynamicOptionsManager.EvaluateDynamicOptionsAsync(expression, formData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task EvaluateMultipleDynamicOptionsAsync_WithMultipleFields_ShouldReturnAllResults()
    {
        // Arrange
        var fieldExpressions = new Dictionary<string, string>
        {
            ["field1"] = "static([{\"value\": \"1\", \"label\": \"Option 1\"}])",
            ["field2"] = "static([{\"value\": \"a\", \"label\": \"Option A\"}])"
        };
        var formData = new Dictionary<string, object?>();

        // Act
        var result = await _dynamicOptionsManager.EvaluateMultipleDynamicOptionsAsync(fieldExpressions, formData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.True(result.Value.ContainsKey("field1"));
        Assert.True(result.Value.ContainsKey("field2"));
        Assert.Single(result.Value["field1"]);
        Assert.Single(result.Value["field2"]);
    }

    [Fact]
    public async Task FilterOptionsAsync_WithValidFilter_ShouldReturnFilteredOptions()
    {
        // Arrange
        var options = new List<FieldOption>
        {
            FieldOption.Create("1", "Apple"),
            FieldOption.Create("2", "Banana"),
            FieldOption.Create("3", "Cherry")
        };
        var filterExpression = "label.contains('a')";
        var formData = new Dictionary<string, object?>();

        // Act
        var result = await _dynamicOptionsManager.FilterOptionsAsync(options, filterExpression, formData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count); // Apple and Banana contain 'a'
    }

    [Fact]
    public async Task ValidateDynamicOptionsExpression_WithValidExpression_ShouldReturnSuccess()
    {
        // Arrange
        var expression = "static([{\"value\": \"1\", \"label\": \"Option 1\"}])";

        // Act
        var result = await _dynamicOptionsManager.ValidateDynamicOptionsExpression(expression);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateDynamicOptionsExpression_WithInvalidExpression_ShouldReturnError()
    {
        // Arrange
        var expression = "invalid_function([])";

        // Act
        var result = await _dynamicOptionsManager.ValidateDynamicOptionsExpression(expression);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetAvailableFunctions_ShouldReturnSupportedFunctions()
    {
        // Act
        var result = await _dynamicOptionsManager.GetAvailableFunctions();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains("static", result.Value);
        Assert.Contains("api", result.Value);
        Assert.Contains("database", result.Value);
        Assert.Contains("if", result.Value);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}