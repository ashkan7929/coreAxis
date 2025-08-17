using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Unit tests for ExpressionEngine.
    /// </summary>
    public class ExpressionEngineTests
    {
        private readonly IExpressionEngine _expressionEngine;
        private readonly Mock<ILogger<ExpressionEngine>> _mockLogger;

        public ExpressionEngineTests()
        {
            _mockLogger = new Mock<ILogger<ExpressionEngine>>();
            _expressionEngine = new ExpressionEngine(_mockLogger.Object);
        }

        [Fact]
        public async Task EvaluateAsync_SimpleArithmetic_ReturnsCorrectResult()
        {
            // Arrange
            var expression = FormulaExpression.Arithmetic("ADD(5, 3)");
            var context = new ExpressionEvaluationContext();

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(8m, result.Value);
            Assert.Equal(typeof(decimal), result.ValueType);
        }

        [Fact]
        public async Task EvaluateAsync_BooleanExpression_ReturnsCorrectResult()
        {
            // Arrange
            var expression = FormulaExpression.Boolean("AND(true, false)");
            var context = new ExpressionEvaluationContext();

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(false, result.Value);
            Assert.Equal(typeof(bool), result.ValueType);
        }

        [Fact]
        public async Task EvaluateAsync_StringExpression_ReturnsCorrectResult()
        {
            // Arrange
            var expression = FormulaExpression.String("CONCAT('Hello', ' World')");
            var context = new ExpressionEvaluationContext();

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Hello World", result.Value);
            Assert.Equal(typeof(string), result.ValueType);
        }

        [Fact]
        public async Task EvaluateAsync_WithVariables_ReturnsCorrectResult()
        {
            // Arrange
            var expression = FormulaExpression.Arithmetic("ADD($x, $y)");
            var context = new ExpressionEvaluationContext();
            context.AddVariable("x", 10);
            context.AddVariable("y", 20);

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(30m, result.Value);
        }

        [Fact]
        public async Task EvaluateAsync_ConditionalExpression_ReturnsCorrectResult()
        {
            // Arrange
            var expression = FormulaExpression.Conditional("IF(GREATER_THAN(10, 5), 'Yes', 'No')");
            var context = new ExpressionEvaluationContext();

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Yes", result.Value);
        }

        [Fact]
        public async Task EvaluateAsync_DateTimeExpression_ReturnsCorrectResult()
        {
            // Arrange
            var expression = FormulaExpression.DateTime("DATE_ADD(NOW(), 1, 'days')");
            var context = new ExpressionEvaluationContext();
            var beforeEvaluation = DateTime.Now;

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.IsType<DateTime>(result.Value);
            var resultDate = (DateTime)result.Value;
            Assert.True(resultDate > beforeEvaluation);
        }

        [Fact]
        public async Task EvaluateAsync_InvalidExpression_ReturnsFailure()
        {
            // Arrange
            var expression = FormulaExpression.Arithmetic("INVALID_FUNCTION(1, 2)");
            var context = new ExpressionEvaluationContext();

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task EvaluateConditionalAsync_TrueCondition_ReturnsTrue()
        {
            // Arrange
            var conditionalLogic = ConditionalLogic.ShowHide("GREATER_THAN(10, 5)", "field1");
            var context = new ExpressionEvaluationContext();

            // Act
            var result = await _expressionEngine.EvaluateConditionalAsync(conditionalLogic, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task EvaluateConditionalAsync_FalseCondition_ReturnsFalse()
        {
            // Arrange
            var conditionalLogic = ConditionalLogic.ShowHide("LESS_THAN(10, 5)", "field1");
            var context = new ExpressionEvaluationContext();

            // Act
            var result = await _expressionEngine.EvaluateConditionalAsync(conditionalLogic, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateExpression_ValidExpression_ReturnsValid()
        {
            // Arrange
            var expression = "ADD(1, 2)";

            // Act
            var result = _expressionEngine.ValidateExpression(expression);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void ValidateExpression_EmptyExpression_ReturnsInvalid()
        {
            // Arrange
            var expression = "";

            // Act
            var result = _expressionEngine.ValidateExpression(expression);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Expression cannot be empty", result.Errors);
        }

        [Fact]
        public void ValidateExpression_UnbalancedParentheses_ReturnsInvalid()
        {
            // Arrange
            var expression = "ADD(1, 2";

            // Act
            var result = _expressionEngine.ValidateExpression(expression);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Unbalanced parentheses in expression", result.Errors);
        }

        [Fact]
        public void IsSafeExpression_SafeExpression_ReturnsTrue()
        {
            // Arrange
            var expression = "ADD(1, 2)";

            // Act
            var result = _expressionEngine.IsSafeExpression(expression);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsSafeExpression_UnsafeExpression_ReturnsFalse()
        {
            // Arrange
            var expression = "System.IO.File.Delete('test.txt')";

            // Act
            var result = _expressionEngine.IsSafeExpression(expression);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetAvailableFunctions_ReturnsExpectedFunctions()
        {
            // Act
            var functions = _expressionEngine.GetAvailableFunctions();

            // Assert
            Assert.NotEmpty(functions);
            Assert.Contains(functions, f => f.Name == "IF");
            Assert.Contains(functions, f => f.Name == "ADD");
            Assert.Contains(functions, f => f.Name == "CONCAT");
            Assert.Contains(functions, f => f.Name == "NOW");
        }

        [Fact]
        public void GetSupportedOperators_ReturnsExpectedOperators()
        {
            // Act
            var operators = _expressionEngine.GetSupportedOperators();

            // Assert
            Assert.NotEmpty(operators);
            Assert.Contains("+", operators);
            Assert.Contains("-", operators);
            Assert.Contains("*", operators);
            Assert.Contains("/", operators);
            Assert.Contains("==", operators);
            Assert.Contains("!=", operators);
            Assert.Contains(">", operators);
            Assert.Contains("<", operators);
        }

        [Theory]
        [InlineData("eval")]
        [InlineData("exec")]
        [InlineData("system")]
        [InlineData("process")]
        [InlineData("file")]
        [InlineData("reflection")]
        public void IsSafeExpression_BlacklistedKeywords_ReturnsFalse(string keyword)
        {
            // Arrange
            var expression = $"SOME_FUNCTION({keyword})";

            // Act
            var result = _expressionEngine.IsSafeExpression(expression);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EvaluateAsync_ComplexNestedExpression_ReturnsCorrectResult()
        {
            // Arrange
            var expression = FormulaExpression.Conditional(
                "IF(AND(GREATER_THAN($age, 18), EQUALS($status, 'active')), 'eligible', 'not_eligible')");
            var context = new ExpressionEvaluationContext();
            context.AddVariable("age", 25);
            context.AddVariable("status", "active");

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("eligible", result.Value);
        }

        [Fact]
        public async Task EvaluateAsync_StringOperations_ReturnsCorrectResult()
        {
            // Arrange
            var expression = FormulaExpression.String("UPPER(TRIM(' hello world '))");
            var context = new ExpressionEvaluationContext();

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("HELLO WORLD", result.Value);
        }

        [Fact]
        public async Task EvaluateAsync_MathOperations_ReturnsCorrectResult()
        {
            // Arrange
            var expression = FormulaExpression.Arithmetic("ROUND(DIVIDE(MULTIPLY(10, 3), 7), 2)");
            var context = new ExpressionEvaluationContext();

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(4.29m, result.Value);
        }

        [Fact]
        public async Task EvaluateAsync_NullHandling_ReturnsCorrectResult()
        {
            // Arrange
            var expression = FormulaExpression.Conditional("IF(IS_NULL($value), 'default', $value)");
            var context = new ExpressionEvaluationContext();
            context.AddVariable("value", null);

            // Act
            var result = await _expressionEngine.EvaluateAsync(expression, context);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("default", result.Value);
        }
    }
}