using CoreAxis.SharedKernel;
using System;
using Xunit;

namespace CoreAxis.Tests.SharedKernel
{
    /// <summary>
    /// Unit tests for the Result class.
    /// </summary>
    public class ResultTests
    {
        /// <summary>
        /// Tests that Success creates a successful result with the provided value.
        /// </summary>
        [Fact]
        public void Success_ShouldCreateSuccessfulResultWithValue()
        {
            // Arrange
            string value = "Test Value";

            // Act
            var result = Result<string>.Success(value);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(value, result.Value);
            Assert.Empty(result.Error);
        }

        /// <summary>
        /// Tests that Failure creates a failed result with the provided error message.
        /// </summary>
        [Fact]
        public void Failure_ShouldCreateFailedResultWithErrorMessage()
        {
            // Arrange
            string errorMessage = "Test Error";

            // Act
            var result = Result<string>.Failure(errorMessage);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(default, result.Value);
            Assert.Equal(errorMessage, result.Error);
        }

        /// <summary>
        /// Tests that Success throws an ArgumentNullException when the value is null.
        /// </summary>
        [Fact]
        public void Success_WithNullValue_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null));
            Assert.Equal("value", exception.ParamName);
        }

        /// <summary>
        /// Tests that Failure throws an ArgumentException when the error message is null or empty.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Failure_WithNullOrEmptyErrorMessage_ShouldThrowArgumentException(string errorMessage)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => Result<string>.Failure(errorMessage));
            Assert.Equal("errorMessage", exception.ParamName);
        }

        /// <summary>
        /// Tests that non-generic Success creates a successful result.
        /// </summary>
        [Fact]
        public void NonGeneric_Success_ShouldCreateSuccessfulResult()
        {
            // Act
            var result = Result.Success();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Error);
        }

        /// <summary>
        /// Tests that non-generic Failure creates a failed result with the provided error message.
        /// </summary>
        [Fact]
        public void NonGeneric_Failure_ShouldCreateFailedResultWithErrorMessage()
        {
            // Arrange
            string errorMessage = "Test Error";

            // Act
            var result = Result.Failure(errorMessage);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(errorMessage, result.Error);
        }

        /// <summary>
        /// Tests that non-generic Failure throws an ArgumentException when the error message is null or empty.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NonGeneric_Failure_WithNullOrEmptyErrorMessage_ShouldThrowArgumentException(string errorMessage)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => Result.Failure(errorMessage));
            Assert.Equal("errorMessage", exception.ParamName);
        }

        /// <summary>
        /// Tests that implicit conversion from value to Result works correctly.
        /// </summary>
        [Fact]
        public void ImplicitConversion_FromValue_ShouldCreateSuccessfulResult()
        {
            // Arrange
            string value = "Test Value";

            // Act
            Result<string> result = value;

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(value, result.Value);
            Assert.Empty(result.Error);
        }

        /// <summary>
        /// Tests that implicit conversion from null value to Result throws an ArgumentNullException.
        /// </summary>
        [Fact]
        public void ImplicitConversion_FromNullValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            string value = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => { Result<string> result = value; });
            Assert.Equal("value", exception.ParamName);
        }
    }
}