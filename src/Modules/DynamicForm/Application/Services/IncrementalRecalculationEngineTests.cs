using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Application.Services;

/// <summary>
/// Unit tests for IncrementalRecalculationEngine
/// </summary>
public class IncrementalRecalculationEngineTests
{
    private readonly Mock<ILogger<IncrementalRecalculationEngine>> _mockLogger;
    private readonly Mock<IDependencyGraph> _mockDependencyGraph;
    private readonly Mock<IExpressionEngine> _mockExpressionEngine;
    private readonly IncrementalRecalculationEngine _engine;

    public IncrementalRecalculationEngineTests()
    {
        _mockLogger = new Mock<ILogger<IncrementalRecalculationEngine>>();
        _mockDependencyGraph = new Mock<IDependencyGraph>();
        _mockExpressionEngine = new Mock<IExpressionEngine>();
        _engine = new IncrementalRecalculationEngine(_mockLogger.Object);
    }

    [Fact]
    public async Task RecalculateAsync_ValidInput_ShouldRecalculateSuccessfully()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            { "price", 100 },
            { "quantity", 2 },
            { "total", 0 }
        };

        var fieldsToRecalculate = new List<string> { "total" };
        _mockDependencyGraph.Setup(x => x.GetFieldsToRecalculate("price"))
            .Returns(fieldsToRecalculate);

        var evaluationResult = new ExpressionEvaluationResult
        {
            IsSuccess = true,
            Value = 200,
            ErrorMessage = null
        };

        _mockExpressionEngine.Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<ExpressionEvaluationContext>()))
            .ReturnsAsync(evaluationResult);

        // Act
        var result = await _engine.RecalculateAsync(
            formData, 
            "price", 
            150, 
            _mockDependencyGraph.Object, 
            _mockExpressionEngine.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150, result["price"]);
        Assert.Equal(200, result["total"]);
        _mockDependencyGraph.Verify(x => x.GetFieldsToRecalculate("price"), Times.Once);
    }

    [Fact]
    public async Task RecalculateAsync_NoFieldsToRecalculate_ShouldReturnUpdatedFormData()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            { "price", 100 },
            { "quantity", 2 }
        };

        _mockDependencyGraph.Setup(x => x.GetFieldsToRecalculate("price"))
            .Returns(new List<string>());

        // Act
        var result = await _engine.RecalculateAsync(
            formData, 
            "price", 
            150, 
            _mockDependencyGraph.Object, 
            _mockExpressionEngine.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150, result["price"]);
        Assert.Equal(2, result["quantity"]);
        _mockExpressionEngine.Verify(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<ExpressionEvaluationContext>()), Times.Never);
    }

    [Fact]
    public async Task RecalculateAsync_ExpressionEvaluationFails_ShouldContinueWithOtherFields()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            { "price", 100 },
            { "quantity", 2 },
            { "total", 0 },
            { "tax", 0 }
        };

        var fieldsToRecalculate = new List<string> { "total", "tax" };
        _mockDependencyGraph.Setup(x => x.GetFieldsToRecalculate("price"))
            .Returns(fieldsToRecalculate);

        var failedResult = new ExpressionEvaluationResult
        {
            IsSuccess = false,
            Value = null,
            ErrorMessage = "Division by zero"
        };

        var successResult = new ExpressionEvaluationResult
        {
            IsSuccess = true,
            Value = 20,
            ErrorMessage = null
        };

        _mockExpressionEngine.SetupSequence(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<ExpressionEvaluationContext>()))
            .ReturnsAsync(failedResult)
            .ReturnsAsync(successResult);

        // Act
        var result = await _engine.RecalculateAsync(
            formData, 
            "price", 
            150, 
            _mockDependencyGraph.Object, 
            _mockExpressionEngine.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150, result["price"]);
        Assert.Equal(0, result["total"]); // Should remain unchanged due to failed evaluation
        Assert.Equal(20, result["tax"]); // Should be updated
    }

    [Fact]
    public async Task RecalculateAllAsync_ValidInput_ShouldRecalculateAllFields()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            { "price", 100 },
            { "quantity", 2 },
            { "total", 0 },
            { "tax", 0 }
        };

        var fieldsInOrder = new List<string> { "price", "quantity", "total", "tax" };
        _mockDependencyGraph.Setup(x => x.GetTopologicalOrder())
            .Returns(fieldsInOrder);

        var evaluationResults = new Queue<ExpressionEvaluationResult>();
        evaluationResults.Enqueue(new ExpressionEvaluationResult { IsSuccess = false }); // price (no expression)
        evaluationResults.Enqueue(new ExpressionEvaluationResult { IsSuccess = false }); // quantity (no expression)
        evaluationResults.Enqueue(new ExpressionEvaluationResult { IsSuccess = true, Value = 200 }); // total
        evaluationResults.Enqueue(new ExpressionEvaluationResult { IsSuccess = true, Value = 20 }); // tax

        _mockExpressionEngine.Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<ExpressionEvaluationContext>()))
            .ReturnsAsync(() => evaluationResults.Dequeue());

        // Act
        var result = await _engine.RecalculateAllAsync(
            formData, 
            _mockDependencyGraph.Object, 
            _mockExpressionEngine.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result["price"]);
        Assert.Equal(2, result["quantity"]);
        Assert.Equal(200, result["total"]);
        Assert.Equal(20, result["tax"]);
        _mockDependencyGraph.Verify(x => x.GetTopologicalOrder(), Times.Once);
    }

    [Fact]
    public async Task RecalculateAllAsync_NoFieldsInGraph_ShouldReturnOriginalData()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            { "price", 100 },
            { "quantity", 2 }
        };

        _mockDependencyGraph.Setup(x => x.GetTopologicalOrder())
            .Returns(new List<string>());

        // Act
        var result = await _engine.RecalculateAllAsync(
            formData, 
            _mockDependencyGraph.Object, 
            _mockExpressionEngine.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result["price"]);
        Assert.Equal(2, result["quantity"]);
        _mockExpressionEngine.Verify(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<ExpressionEvaluationContext>()), Times.Never);
    }

    [Fact]
    public async Task RecalculateAsync_NullFormData_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _engine.RecalculateAsync(
                null!, 
                "field", 
                "value", 
                _mockDependencyGraph.Object, 
                _mockExpressionEngine.Object));
    }

    [Fact]
    public async Task RecalculateAsync_EmptyChangedField_ShouldThrowArgumentException()
    {
        // Arrange
        var formData = new Dictionary<string, object?>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _engine.RecalculateAsync(
                formData, 
                "", 
                "value", 
                _mockDependencyGraph.Object, 
                _mockExpressionEngine.Object));
    }

    [Fact]
    public async Task RecalculateAsync_NullDependencyGraph_ShouldThrowArgumentNullException()
    {
        // Arrange
        var formData = new Dictionary<string, object?>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _engine.RecalculateAsync(
                formData, 
                "field", 
                "value", 
                null!, 
                _mockExpressionEngine.Object));
    }

    [Fact]
    public async Task RecalculateAsync_NullExpressionEngine_ShouldThrowArgumentNullException()
    {
        // Arrange
        var formData = new Dictionary<string, object?>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _engine.RecalculateAsync(
                formData, 
                "field", 
                "value", 
                _mockDependencyGraph.Object, 
                null!));
    }

    [Fact]
    public void GetMetrics_InitialState_ShouldReturnEmptyMetrics()
    {
        // Act
        var metrics = _engine.GetMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.TotalRecalculations);
        Assert.Equal(TimeSpan.Zero, metrics.TotalCalculationTime);
        Assert.Equal(TimeSpan.Zero, metrics.AverageCalculationTime);
        Assert.Equal(0, metrics.FieldsRecalculated);
        Assert.Empty(metrics.FieldRecalculationCounts);
        Assert.Empty(metrics.FieldCalculationTimes);
    }

    [Fact]
    public async Task GetMetrics_AfterRecalculation_ShouldReturnUpdatedMetrics()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            { "price", 100 },
            { "total", 0 }
        };

        var fieldsToRecalculate = new List<string> { "total" };
        _mockDependencyGraph.Setup(x => x.GetFieldsToRecalculate("price"))
            .Returns(fieldsToRecalculate);

        var evaluationResult = new ExpressionEvaluationResult
        {
            IsSuccess = true,
            Value = 200
        };

        _mockExpressionEngine.Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<ExpressionEvaluationContext>()))
            .ReturnsAsync(evaluationResult);

        // Act
        await _engine.RecalculateAsync(
            formData, 
            "price", 
            150, 
            _mockDependencyGraph.Object, 
            _mockExpressionEngine.Object);

        var metrics = _engine.GetMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.TotalRecalculations);
        Assert.True(metrics.TotalCalculationTime > TimeSpan.Zero);
        Assert.Equal(1, metrics.FieldsRecalculated);
        Assert.True(metrics.LastRecalculation > DateTime.MinValue);
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new IncrementalRecalculationEngine(null!));
    }

    [Fact]
    public async Task RecalculateAsync_ComplexDependencyChain_ShouldRecalculateInCorrectOrder()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            { "price", 100 },
            { "quantity", 2 },
            { "total", 0 },
            { "tax", 0 },
            { "grandTotal", 0 }
        };

        // price -> total -> tax, grandTotal
        var fieldsToRecalculate = new List<string> { "total", "tax", "grandTotal" };
        _mockDependencyGraph.Setup(x => x.GetFieldsToRecalculate("price"))
            .Returns(fieldsToRecalculate);

        var evaluationResults = new Queue<ExpressionEvaluationResult>();
        evaluationResults.Enqueue(new ExpressionEvaluationResult { IsSuccess = true, Value = 200 }); // total
        evaluationResults.Enqueue(new ExpressionEvaluationResult { IsSuccess = true, Value = 20 }); // tax
        evaluationResults.Enqueue(new ExpressionEvaluationResult { IsSuccess = true, Value = 220 }); // grandTotal

        _mockExpressionEngine.Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<ExpressionEvaluationContext>()))
            .ReturnsAsync(() => evaluationResults.Dequeue());

        // Act
        var result = await _engine.RecalculateAsync(
            formData, 
            "price", 
            150, 
            _mockDependencyGraph.Object, 
            _mockExpressionEngine.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150, result["price"]);
        Assert.Equal(200, result["total"]);
        Assert.Equal(20, result["tax"]);
        Assert.Equal(220, result["grandTotal"]);
        
        // Verify that expression engine was called for each field
        _mockExpressionEngine.Verify(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<ExpressionEvaluationContext>()), Times.Exactly(3));
    }
}