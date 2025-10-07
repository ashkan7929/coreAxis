using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Common;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.Modules.DynamicForm.Application.Services;

public class FormulaServiceTests
{
    private readonly Mock<IFormulaDefinitionRepository> _formulaDefinitionRepositoryMock;
    private readonly Mock<IFormulaVersionRepository> _formulaVersionRepositoryMock;
    private readonly Mock<IFormulaEvaluationLogRepository> _evaluationLogRepositoryMock;
    private readonly Mock<IExpressionEngine> _expressionEngineMock;
    private readonly Mock<ILogger<FormulaService>> _loggerMock;
    private readonly FormulaService _formulaService;

    public FormulaServiceTests()
    {
        _formulaDefinitionRepositoryMock = new Mock<IFormulaDefinitionRepository>();
        _formulaVersionRepositoryMock = new Mock<IFormulaVersionRepository>();
        _evaluationLogRepositoryMock = new Mock<IFormulaEvaluationLogRepository>();
        _expressionEngineMock = new Mock<IExpressionEngine>();
        _loggerMock = new Mock<ILogger<FormulaService>>();

        _formulaService = new FormulaService(
            _formulaDefinitionRepositoryMock.Object,
            _formulaVersionRepositoryMock.Object,
            _evaluationLogRepositoryMock.Object,
            _expressionEngineMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task EvaluateFormulaAsync_WithValidFormulaId_ShouldReturnSuccessResult()
    {
        // Arrange
        var formulaId = Guid.NewGuid();
        var inputs = new Dictionary<string, object> { { "x", 10 }, { "y", 20 } };
        var context = new Dictionary<string, object>();
        var expectedResult = 30.0;

        var formulaDefinition = new FormulaDefinition("TestFormula", "Test formula", "Math");
        var formulaVersion = new FormulaVersion(formulaDefinition.Id, 1, "x + y", "Test version");
        formulaVersion.Publish();

        var evaluationResult = new ExpressionEvaluationResult
        {
            Value = expectedResult,
            IsSuccess = true,
            ExecutionTime = TimeSpan.FromMilliseconds(10)
        };

        _formulaDefinitionRepositoryMock
            .Setup(x => x.GetByIdAsync(formulaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formulaDefinition);

        _formulaVersionRepositoryMock
            .Setup(x => x.GetLatestPublishedEffectiveVersionAsync(formulaId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(formulaVersion);

        _expressionEngineMock
            .Setup(x => x.EvaluateAsync(It.IsAny<FormulaExpression>(), It.IsAny<ExpressionEvaluationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationResult);

        _evaluationLogRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<FormulaEvaluationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _formulaService.EvaluateFormulaAsync(formulaId, inputs, context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedResult, result.Value.Value);
        Assert.Equal(formulaVersion.VersionNumber, result.Value.FormulaVersion);
        Assert.NotNull(result.Value.Metadata);

        _evaluationLogRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<FormulaEvaluationLog>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateFormulaAsync_WithInvalidFormulaId_ShouldReturnFailureResult()
    {
        // Arrange
        var formulaId = Guid.NewGuid();
        var inputs = new Dictionary<string, object>();
        var context = new Dictionary<string, object>();

        _formulaDefinitionRepositoryMock
            .Setup(x => x.GetByIdAsync(formulaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FormulaDefinition?)null);

        // Act
        var result = await _formulaService.EvaluateFormulaAsync(formulaId, inputs, context);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Formula not found", result.Error);
    }

    [Fact]
    public async Task EvaluateFormulaAsync_WithFormulaName_ShouldReturnSuccessResult()
    {
        // Arrange
        var formulaName = "TestFormula";
        var version = 1;
        var inputs = new Dictionary<string, object> { { "x", 5 }, { "y", 15 } };
        var context = new Dictionary<string, object>();
        var expectedResult = 20.0;

        var formulaDefinition = new FormulaDefinition(formulaName, "Test formula", "Math");
        var formulaVersion = new FormulaVersion(formulaDefinition.Id, version, "x + y", "Test version");
        formulaVersion.Publish();

        var evaluationResult = new ExpressionEvaluationResult
        {
            Value = expectedResult,
            IsSuccess = true,
            ExecutionTime = TimeSpan.FromMilliseconds(15)
        };

        _formulaDefinitionRepositoryMock
            .Setup(x => x.GetByNameAsync(formulaName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formulaDefinition);

        _formulaVersionRepositoryMock
            .Setup(x => x.GetByFormulaDefinitionIdAndVersionAsync(formulaDefinition.Id, version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formulaVersion);

        _expressionEngineMock
            .Setup(x => x.EvaluateAsync(It.IsAny<FormulaExpression>(), It.IsAny<ExpressionEvaluationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationResult);

        _evaluationLogRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<FormulaEvaluationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _formulaService.EvaluateFormulaAsync(formulaName, version, inputs, context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedResult, result.Value.Value);
        Assert.Equal(version, result.Value.FormulaVersion);
    }

    [Fact]
    public async Task GetLatestPublishedVersionAsync_WithValidFormulaId_ShouldReturnSuccessResult()
    {
        // Arrange
        var formulaId = Guid.NewGuid();
        var formulaDefinition = new FormulaDefinition("TestFormula", "Test formula", "Math");
        var formulaVersion = new FormulaVersion(formulaDefinition.Id, 1, "x + y", "Test version");
        formulaVersion.Publish();

        _formulaDefinitionRepositoryMock
            .Setup(x => x.GetByIdAsync(formulaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formulaDefinition);

        _formulaVersionRepositoryMock
            .Setup(x => x.GetLatestPublishedEffectiveVersionAsync(formulaId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(formulaVersion);

        // Act
        var result = await _formulaService.GetLatestPublishedVersionAsync(formulaId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(formulaVersion.VersionNumber, result.Value.VersionNumber);
        Assert.Equal(formulaVersion.Expression, result.Value.Expression);
    }

    [Fact]
    public async Task GetAvailableFunctionsAsync_ShouldReturnSuccessResult()
    {
        // Arrange
        var expectedFunctions = new List<FunctionSignature>
        {
            new FunctionSignature("ADD", "Addition function", new[] { "number", "number" }, "number"),
            new FunctionSignature("SUBTRACT", "Subtraction function", new[] { "number", "number" }, "number")
        };

        _expressionEngineMock
            .Setup(x => x.GetAvailableFunctions())
            .Returns(expectedFunctions);

        // Act
        var result = await _formulaService.GetAvailableFunctionsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedFunctions.Count, result.Value.Count());
    }

    [Fact]
    public async Task ValidateExpressionAsync_WithValidExpression_ShouldReturnTrue()
    {
        // Arrange
        var expression = "x + y";
        var context = new Dictionary<string, object>();

        _expressionEngineMock
            .Setup(x => x.EvaluateAsync(It.IsAny<FormulaExpression>(), It.IsAny<ExpressionEvaluationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExpressionEvaluationResult { IsSuccess = true, Value = 0 });

        // Act
        var result = await _formulaService.ValidateExpressionAsync(expression, context);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task ValidateExpressionAsync_WithInvalidExpression_ShouldReturnFalse()
    {
        // Arrange
        var expression = "invalid expression";
        var context = new Dictionary<string, object>();

        _expressionEngineMock
            .Setup(x => x.EvaluateAsync(It.IsAny<FormulaExpression>(), It.IsAny<ExpressionEvaluationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExpressionEvaluationResult { IsSuccess = false, ErrorMessage = "Invalid syntax" });

        // Act
        var result = await _formulaService.ValidateExpressionAsync(expression, context);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid syntax", result.Error);
    }

    [Fact]
    public async Task GetEvaluationHistoryAsync_WithValidFormulaId_ShouldReturnPagedResult()
    {
        // Arrange
        var formulaId = Guid.NewGuid();
        var pageNumber = 1;
        var pageSize = 10;

        var evaluationLogs = new List<FormulaEvaluationLog>
        {
            new FormulaEvaluationLog(formulaId, 1, "x + y", new Dictionary<string, object>(), 30.0, TimeSpan.FromMilliseconds(10)),
            new FormulaEvaluationLog(formulaId, 1, "x + y", new Dictionary<string, object>(), 50.0, TimeSpan.FromMilliseconds(12))
        }.AsQueryable();

        _evaluationLogRepositoryMock
            .Setup(x => x.GetByFormulaDefinitionIdAsync(formulaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationLogs);

        // Act
        var result = await _formulaService.GetEvaluationHistoryAsync(formulaId, pageNumber, pageSize);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(pageNumber, result.Value.PageNumber);
        Assert.Equal(pageSize, result.Value.PageSize);
    }

    [Fact]
    public async Task GetPerformanceMetricsAsync_WithValidFormulaId_ShouldReturnMetrics()
    {
        // Arrange
        var formulaId = Guid.NewGuid();
        var formulaDefinition = new FormulaDefinition("TestFormula", "Test formula", "Math");

        var evaluationLogs = new List<FormulaEvaluationLog>
        {
            new FormulaEvaluationLog(formulaId, 1, "x + y", new Dictionary<string, object>(), 30.0, TimeSpan.FromMilliseconds(10)),
            new FormulaEvaluationLog(formulaId, 1, "x + y", new Dictionary<string, object>(), 50.0, TimeSpan.FromMilliseconds(15)),
            new FormulaEvaluationLog(formulaId, 1, "x + y", new Dictionary<string, object>(), 40.0, TimeSpan.FromMilliseconds(12))
        }.AsQueryable();

        _formulaDefinitionRepositoryMock
            .Setup(x => x.GetByIdAsync(formulaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(formulaDefinition);

        _evaluationLogRepositoryMock
            .Setup(x => x.GetByFormulaDefinitionIdAsync(formulaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationLogs);

        // Act
        var result = await _formulaService.GetPerformanceMetricsAsync(formulaId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.TotalEvaluations);
        Assert.Equal(12.33, Math.Round(result.Value.AverageExecutionTime, 2));
        Assert.Equal(10, result.Value.MinExecutionTime);
        Assert.Equal(15, result.Value.MaxExecutionTime);
    }
}