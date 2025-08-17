using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Infrastructure.Persistence;
using CoreAxis.Modules.DynamicForm.Infrastructure.Repositories;
using CoreAxis.Modules.DynamicForm.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.Modules.DynamicForm.Integration;

public class FormulaEngineIntegrationTests : IClassFixture<FormulaEngineTestFixture>
{
    private readonly FormulaEngineTestFixture _fixture;
    private readonly IFormulaService _formulaService;
    private readonly IFormulaDefinitionRepository _formulaDefinitionRepository;
    private readonly IFormulaVersionRepository _formulaVersionRepository;
    private readonly DynamicFormDbContext _dbContext;

    public FormulaEngineIntegrationTests(FormulaEngineTestFixture fixture)
    {
        _fixture = fixture;
        _formulaService = _fixture.ServiceProvider.GetRequiredService<IFormulaService>();
        _formulaDefinitionRepository = _fixture.ServiceProvider.GetRequiredService<IFormulaDefinitionRepository>();
        _formulaVersionRepository = _fixture.ServiceProvider.GetRequiredService<IFormulaVersionRepository>();
        _dbContext = _fixture.ServiceProvider.GetRequiredService<DynamicFormDbContext>();
    }

    [Fact]
    public async Task FormulaEngine_EndToEndEvaluation_ShouldWorkCorrectly()
    {
        // Arrange
        var formulaDefinition = new FormulaDefinition("TaxCalculation", "Calculate tax amount", "Finance");
        await _formulaDefinitionRepository.AddAsync(formulaDefinition);
        await _dbContext.SaveChangesAsync();

        var formulaVersion = new FormulaVersion(
            formulaDefinition.Id,
            1,
            "amount * taxRate",
            "Calculate tax based on amount and tax rate");
        formulaVersion.Publish();
        
        await _formulaVersionRepository.AddAsync(formulaVersion);
        await _dbContext.SaveChangesAsync();

        var inputs = new Dictionary<string, object>
        {
            { "amount", 1000.0 },
            { "taxRate", 0.15 }
        };

        // Act
        var result = await _formulaService.EvaluateFormulaAsync(
            formulaDefinition.Id,
            inputs,
            new Dictionary<string, object>());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(150.0, result.Value.Value);
        Assert.Equal(1, result.Value.FormulaVersion);
        Assert.NotNull(result.Value.Metadata);
        Assert.True(result.Value.Metadata.ContainsKey("executionTime"));
    }

    [Fact]
    public async Task FormulaEngine_ComplexExpression_ShouldEvaluateCorrectly()
    {
        // Arrange
        var formulaDefinition = new FormulaDefinition("ComplexCalculation", "Complex mathematical calculation", "Math");
        await _formulaDefinitionRepository.AddAsync(formulaDefinition);
        await _dbContext.SaveChangesAsync();

        var formulaVersion = new FormulaVersion(
            formulaDefinition.Id,
            1,
            "(base + bonus) * multiplier - deduction",
            "Complex calculation with multiple operations");
        formulaVersion.Publish();
        
        await _formulaVersionRepository.AddAsync(formulaVersion);
        await _dbContext.SaveChangesAsync();

        var inputs = new Dictionary<string, object>
        {
            { "base", 5000.0 },
            { "bonus", 1000.0 },
            { "multiplier", 1.2 },
            { "deduction", 500.0 }
        };

        // Act
        var result = await _formulaService.EvaluateFormulaAsync(
            formulaDefinition.Id,
            inputs,
            new Dictionary<string, object>());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(6700.0, result.Value.Value); // (5000 + 1000) * 1.2 - 500 = 6700
    }

    [Fact]
    public async Task FormulaEngine_EvaluationByName_ShouldWorkCorrectly()
    {
        // Arrange
        var formulaName = "DiscountCalculation";
        var formulaDefinition = new FormulaDefinition(formulaName, "Calculate discount amount", "Sales");
        await _formulaDefinitionRepository.AddAsync(formulaDefinition);
        await _dbContext.SaveChangesAsync();

        var formulaVersion = new FormulaVersion(
            formulaDefinition.Id,
            1,
            "price * discountRate",
            "Calculate discount based on price and discount rate");
        formulaVersion.Publish();
        
        await _formulaVersionRepository.AddAsync(formulaVersion);
        await _dbContext.SaveChangesAsync();

        var inputs = new Dictionary<string, object>
        {
            { "price", 200.0 },
            { "discountRate", 0.10 }
        };

        // Act
        var result = await _formulaService.EvaluateFormulaAsync(
            formulaName,
            1,
            inputs,
            new Dictionary<string, object>());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(20.0, result.Value.Value);
        Assert.Equal(1, result.Value.FormulaVersion);
    }

    [Fact]
    public async Task FormulaEngine_GetLatestPublishedVersion_ShouldReturnCorrectVersion()
    {
        // Arrange
        var formulaDefinition = new FormulaDefinition("VersionTest", "Test versioning", "Test");
        await _formulaDefinitionRepository.AddAsync(formulaDefinition);
        await _dbContext.SaveChangesAsync();

        // Create multiple versions
        var version1 = new FormulaVersion(formulaDefinition.Id, 1, "x + y", "Version 1");
        version1.Publish();
        await _formulaVersionRepository.AddAsync(version1);

        var version2 = new FormulaVersion(formulaDefinition.Id, 2, "x * y", "Version 2");
        version2.Publish();
        await _formulaVersionRepository.AddAsync(version2);

        var version3 = new FormulaVersion(formulaDefinition.Id, 3, "x - y", "Version 3 (Draft)");
        // Don't publish version 3
        await _formulaVersionRepository.AddAsync(version3);

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _formulaService.GetLatestPublishedVersionAsync(formulaDefinition.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.VersionNumber);
        Assert.Equal("x * y", result.Value.Expression);
    }

    [Fact]
    public async Task FormulaEngine_GetAvailableFunctions_ShouldReturnFunctions()
    {
        // Act
        var result = await _formulaService.GetAvailableFunctionsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value);
        
        // Check for some basic mathematical functions
        var functionNames = result.Value.Select(f => f.Name).ToList();
        Assert.Contains("ADD", functionNames);
        Assert.Contains("SUBTRACT", functionNames);
        Assert.Contains("MULTIPLY", functionNames);
        Assert.Contains("DIVIDE", functionNames);
    }

    [Fact]
    public async Task FormulaEngine_ValidateExpression_ShouldWorkCorrectly()
    {
        // Act & Assert - Valid expression
        var validResult = await _formulaService.ValidateExpressionAsync(
            "x + y * 2",
            new Dictionary<string, object>());
        
        Assert.True(validResult.IsSuccess);
        Assert.True(validResult.Value);

        // Act & Assert - Invalid expression
        var invalidResult = await _formulaService.ValidateExpressionAsync(
            "x + + y",
            new Dictionary<string, object>());
        
        Assert.False(invalidResult.IsSuccess);
    }

    [Fact]
    public async Task FormulaEngine_EvaluationHistory_ShouldBeTracked()
    {
        // Arrange
        var formulaDefinition = new FormulaDefinition("HistoryTest", "Test evaluation history", "Test");
        await _formulaDefinitionRepository.AddAsync(formulaDefinition);
        await _dbContext.SaveChangesAsync();

        var formulaVersion = new FormulaVersion(formulaDefinition.Id, 1, "x + y", "History test");
        formulaVersion.Publish();
        await _formulaVersionRepository.AddAsync(formulaVersion);
        await _dbContext.SaveChangesAsync();

        var inputs = new Dictionary<string, object> { { "x", 10 }, { "y", 20 } };

        // Act - Perform multiple evaluations
        await _formulaService.EvaluateFormulaAsync(formulaDefinition.Id, inputs, new Dictionary<string, object>());
        await _formulaService.EvaluateFormulaAsync(formulaDefinition.Id, inputs, new Dictionary<string, object>());
        await _formulaService.EvaluateFormulaAsync(formulaDefinition.Id, inputs, new Dictionary<string, object>());

        // Get evaluation history
        var historyResult = await _formulaService.GetEvaluationHistoryAsync(formulaDefinition.Id, 1, 10);

        // Assert
        Assert.True(historyResult.IsSuccess);
        Assert.NotNull(historyResult.Value);
        Assert.Equal(3, historyResult.Value.TotalCount);
        Assert.Equal(3, historyResult.Value.Items.Count);
        
        // Check that all evaluations have the same result
        Assert.All(historyResult.Value.Items, log => Assert.Equal(30.0, log.Result));
    }

    [Fact]
    public async Task FormulaEngine_PerformanceMetrics_ShouldBeCalculated()
    {
        // Arrange
        var formulaDefinition = new FormulaDefinition("MetricsTest", "Test performance metrics", "Test");
        await _formulaDefinitionRepository.AddAsync(formulaDefinition);
        await _dbContext.SaveChangesAsync();

        var formulaVersion = new FormulaVersion(formulaDefinition.Id, 1, "x * y", "Metrics test");
        formulaVersion.Publish();
        await _formulaVersionRepository.AddAsync(formulaVersion);
        await _dbContext.SaveChangesAsync();

        var inputs = new Dictionary<string, object> { { "x", 5 }, { "y", 10 } };

        // Act - Perform multiple evaluations
        for (int i = 0; i < 5; i++)
        {
            await _formulaService.EvaluateFormulaAsync(formulaDefinition.Id, inputs, new Dictionary<string, object>());
        }

        // Get performance metrics
        var metricsResult = await _formulaService.GetPerformanceMetricsAsync(formulaDefinition.Id);

        // Assert
        Assert.True(metricsResult.IsSuccess);
        Assert.NotNull(metricsResult.Value);
        Assert.Equal(5, metricsResult.Value.TotalEvaluations);
        Assert.True(metricsResult.Value.AverageExecutionTime > 0);
        Assert.True(metricsResult.Value.MinExecutionTime > 0);
        Assert.True(metricsResult.Value.MaxExecutionTime > 0);
        Assert.True(metricsResult.Value.MinExecutionTime <= metricsResult.Value.AverageExecutionTime);
        Assert.True(metricsResult.Value.AverageExecutionTime <= metricsResult.Value.MaxExecutionTime);
    }
}

public class FormulaEngineTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    private readonly DynamicFormDbContext _dbContext;

    public FormulaEngineTestFixture()
    {
        var services = new ServiceCollection();
        
        // Add DbContext with in-memory database
        services.AddDbContext<DynamicFormDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Add repositories
        services.AddScoped<IFormulaDefinitionRepository, FormulaDefinitionRepository>();
        services.AddScoped<IFormulaVersionRepository, FormulaVersionRepository>();
        services.AddScoped<IFormulaEvaluationLogRepository, FormulaEvaluationLogRepository>();

        // Add expression engine
        services.AddScoped<IExpressionEngine, ExpressionEngine>();

        // Add formula service
        services.AddScoped<IFormulaService, FormulaService>();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        ServiceProvider = services.BuildServiceProvider();
        
        // Initialize database
        _dbContext = ServiceProvider.GetRequiredService<DynamicFormDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        (ServiceProvider as IDisposable)?.Dispose();
    }
}