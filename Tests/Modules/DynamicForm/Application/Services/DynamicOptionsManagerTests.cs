using CoreAxis.Modules.ApiManager.Application.Services;
using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.SharedKernel.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.Modules.DynamicForm.Application.Services
{
    public class DynamicOptionsManagerTests
    {
        private readonly Mock<IExpressionEngine> _mockExpressionEngine;
        private readonly Mock<IApiManager> _mockApiManager;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<ILogger<DynamicOptionsManager>> _mockLogger;
        private readonly DynamicOptionsManager _dynamicOptionsManager;

        public DynamicOptionsManagerTests()
        {
            _mockExpressionEngine = new Mock<IExpressionEngine>();
            _mockApiManager = new Mock<IApiManager>();
            _mockCache = new Mock<IMemoryCache>();
            _mockLogger = new Mock<ILogger<DynamicOptionsManager>>();

            _dynamicOptionsManager = new DynamicOptionsManager(
                _mockExpressionEngine.Object,
                _mockApiManager.Object,
                _mockCache.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task EvaluateDynamicOptionsAsync_WithValidExpression_ReturnsSuccess()
        {
            // Arrange
            var expression = "static([{\"value\": \"1\", \"label\": \"Option 1\"}])";
            var formData = new Dictionary<string, object?> { ["field1"] = "value1" };
            var expectedOptions = new List<FieldOption>
            {
                FieldOption.Create("1", "Option 1")
            };

            _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
                .Returns(false);

            _mockExpressionEngine.Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<object>.Success(expectedOptions));

            // Act
            var result = await _dynamicOptionsManager.EvaluateDynamicOptionsAsync(expression, formData);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Equal("1", result.Value[0].Value);
            Assert.Equal("Option 1", result.Value[0].Label);
        }

        [Fact]
        public async Task EvaluateDynamicOptionsAsync_WithNullExpression_ReturnsFailure()
        {
            // Arrange
            string expression = null!;
            var formData = new Dictionary<string, object?>();

            // Act
            var result = await _dynamicOptionsManager.EvaluateDynamicOptionsAsync(expression, formData);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Expression cannot be null or empty", result.Error);
        }

        [Fact]
        public async Task EvaluateDynamicOptionsAsync_WithEmptyExpression_ReturnsFailure()
        {
            // Arrange
            var expression = "";
            var formData = new Dictionary<string, object?>();

            // Act
            var result = await _dynamicOptionsManager.EvaluateDynamicOptionsAsync(expression, formData);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Expression cannot be null or empty", result.Error);
        }

        [Fact]
        public async Task EvaluateDynamicOptionsAsync_WithCachedResult_ReturnsCachedOptions()
        {
            // Arrange
            var expression = "test_expression";
            var formData = new Dictionary<string, object?> { ["field1"] = "value1" };
            var cachedOptions = new List<FieldOption>
            {
                FieldOption.Create("cached_1", "Cached Option 1")
            };

            object? cacheValue = cachedOptions;
            _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
                .Returns(true);

            // Act
            var result = await _dynamicOptionsManager.EvaluateDynamicOptionsAsync(expression, formData);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Equal("cached_1", result.Value[0].Value);
            Assert.Equal("Cached Option 1", result.Value[0].Label);

            // Verify that expression engine was not called
            _mockExpressionEngine.Verify(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EvaluateMultipleDynamicOptionsAsync_WithValidExpressions_ReturnsSuccess()
        {
            // Arrange
            var fieldExpressions = new Dictionary<string, string>
            {
                ["field1"] = "expression1",
                ["field2"] = "expression2"
            };
            var formData = new Dictionary<string, object?>();

            var options1 = new List<FieldOption> { FieldOption.Create("1", "Option 1") };
            var options2 = new List<FieldOption> { FieldOption.Create("2", "Option 2") };

            _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
                .Returns(false);

            _mockExpressionEngine.SetupSequence(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<object>.Success(options1))
                .ReturnsAsync(Result<object>.Success(options2));

            // Act
            var result = await _dynamicOptionsManager.EvaluateMultipleDynamicOptionsAsync(fieldExpressions, formData);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);
            Assert.Contains("field1", result.Value.Keys);
            Assert.Contains("field2", result.Value.Keys);
        }

        [Fact]
        public async Task GetOptionsFromApiAsync_WithValidEndpoint_ReturnsSuccess()
        {
            // Arrange
            var apiEndpoint = "https://api.example.com/options";
            var parameters = new Dictionary<string, object?> { ["param1"] = "value1" };
            var apiResponse = "[{\"value\": \"1\", \"label\": \"API Option 1\"}]";

            _mockApiManager.Setup(x => x.CallApiAsync(apiEndpoint, parameters, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<object>.Success(apiResponse));

            // Act
            var result = await _dynamicOptionsManager.GetOptionsFromApiAsync(apiEndpoint, parameters);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            // Note: The actual parsing logic would need to be implemented properly
        }

        [Fact]
        public async Task GetOptionsFromApiAsync_WithFailedApiCall_ReturnsFailure()
        {
            // Arrange
            var apiEndpoint = "https://api.example.com/options";
            var parameters = new Dictionary<string, object?>();

            _mockApiManager.Setup(x => x.CallApiAsync(apiEndpoint, parameters, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<object>.Failure("API call failed"));

            // Act
            var result = await _dynamicOptionsManager.GetOptionsFromApiAsync(apiEndpoint, parameters);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("API call failed", result.Error);
        }

        [Fact]
        public async Task FilterOptionsAsync_WithValidFilter_ReturnsFilteredOptions()
        {
            // Arrange
            var options = new List<FieldOption>
            {
                FieldOption.Create("1", "Option 1"),
                FieldOption.Create("2", "Option 2"),
                FieldOption.Create("3", "Option 3")
            };
            var filterExpression = "option.value != '2'";
            var formData = new Dictionary<string, object?>();

            _mockExpressionEngine.SetupSequence(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<object>.Success(true))  // Option 1 passes
                .ReturnsAsync(Result<object>.Success(false)) // Option 2 fails
                .ReturnsAsync(Result<object>.Success(true)); // Option 3 passes

            // Act
            var result = await _dynamicOptionsManager.FilterOptionsAsync(options, filterExpression, formData);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);
            Assert.DoesNotContain(result.Value, opt => opt.Value == "2");
        }

        [Fact]
        public async Task FilterOptionsAsync_WithNullOptions_ReturnsEmptyList()
        {
            // Arrange
            List<FieldOption>? options = null;
            var filterExpression = "true";
            var formData = new Dictionary<string, object?>();

            // Act
            var result = await _dynamicOptionsManager.FilterOptionsAsync(options!, filterExpression, formData);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task FilterOptionsAsync_WithEmptyFilter_ReturnsAllOptions()
        {
            // Arrange
            var options = new List<FieldOption>
            {
                FieldOption.Create("1", "Option 1"),
                FieldOption.Create("2", "Option 2")
            };
            var filterExpression = "";
            var formData = new Dictionary<string, object?>();

            // Act
            var result = await _dynamicOptionsManager.FilterOptionsAsync(options, filterExpression, formData);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);
        }

        [Fact]
        public void ValidateDynamicOptionsExpression_WithValidExpression_ReturnsSuccess()
        {
            // Arrange
            var expression = "valid_expression";
            _mockExpressionEngine.Setup(x => x.ValidateExpression(expression))
                .Returns(Result<bool>.Success(true));

            // Act
            var result = _dynamicOptionsManager.ValidateDynamicOptionsExpression(expression);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
        }

        [Fact]
        public void ValidateDynamicOptionsExpression_WithInvalidExpression_ReturnsFailure()
        {
            // Arrange
            var expression = "invalid_expression";
            _mockExpressionEngine.Setup(x => x.ValidateExpression(expression))
                .Returns(Result<bool>.Failure("Invalid syntax"));

            // Act
            var result = _dynamicOptionsManager.ValidateDynamicOptionsExpression(expression);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Invalid syntax", result.Error);
        }

        [Fact]
        public void ValidateDynamicOptionsExpression_WithNullExpression_ReturnsFailure()
        {
            // Arrange
            string expression = null!;

            // Act
            var result = _dynamicOptionsManager.ValidateDynamicOptionsExpression(expression);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("Expression cannot be null or empty", result.Error);
        }

        [Fact]
        public void GetAvailableFunctions_ReturnsExpectedFunctions()
        {
            // Act
            var functions = _dynamicOptionsManager.GetAvailableFunctions();

            // Assert
            Assert.NotNull(functions);
            Assert.True(functions.Count > 0);
            Assert.Contains("api", functions.Keys);
            Assert.Contains("database", functions.Keys);
            Assert.Contains("filter", functions.Keys);
            Assert.Contains("map", functions.Keys);
            Assert.Contains("sort", functions.Keys);
            Assert.Contains("group", functions.Keys);
            Assert.Contains("limit", functions.Keys);
            Assert.Contains("distinct", functions.Keys);
            Assert.Contains("conditional", functions.Keys);
        }

        [Fact]
        public async Task GetOptionsFromDatabaseAsync_WithValidQuery_ReturnsSuccess()
        {
            // Arrange
            var query = "SELECT value, label FROM options";
            var parameters = new Dictionary<string, object?> { ["param1"] = "value1" };

            // Act
            var result = await _dynamicOptionsManager.GetOptionsFromDatabaseAsync(query, parameters);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            // Note: This is a placeholder implementation, so we just verify it doesn't throw
        }
    }
}