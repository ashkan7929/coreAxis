using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Exceptions;
using System;
using System.Collections.Generic;
using Xunit;

namespace CoreAxis.Tests.SharedKernel
{
    /// <summary>
    /// Unit tests for the CoreAxisException classes.
    /// </summary>
    public class CoreAxisExceptionTests
    {
        /// <summary>
        /// Tests that concrete CoreAxisException implementations inherit the Code property.
        /// </summary>
        [Fact]
        public void CoreAxisException_ConcreteImplementation_ShouldHaveCode()
        {
            // Arrange & Act
            var exception = new ValidationException("Test message");

            // Assert
            Assert.Equal("VALIDATION_ERROR", exception.Code);
        }

        /// <summary>
        /// Tests that EntityNotFoundException can be created with an entity name and ID.
        /// </summary>
        [Fact]
        public void EntityNotFoundException_WithEntityNameAndId_ShouldSetMessageWithEntityNameAndId()
        {
            // Arrange
            string entityName = "TestEntity";
            object entityId = Guid.NewGuid();

            // Act
            var exception = new EntityNotFoundException(entityName, entityId);

            // Assert
            Assert.Contains(entityName, exception.Message);
            Assert.Contains(entityId.ToString(), exception.Message);
            Assert.Equal(entityName, exception.EntityName);
            Assert.Equal(entityId, exception.EntityId);
        }

        /// <summary>
        /// Tests that BusinessRuleViolationException can be created with a message.
        /// </summary>
        [Fact]
        public void BusinessRuleViolationException_WithMessage_ShouldSetMessage()
        {
            // Arrange
            string message = "Business rule violated";

            // Act
            var exception = new BusinessRuleViolationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        /// <summary>
        /// Tests that UnauthorizedAccessException can be created with a message.
        /// </summary>
        [Fact]
        public void UnauthorizedAccessException_WithMessage_ShouldSetMessage()
        {
            // Arrange
            string message = "Unauthorized access";

            // Act
            var exception = new CoreAxis.SharedKernel.Exceptions.UnauthorizedAccessException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        /// <summary>
        /// Tests that ValidationException can be created with a message.
        /// </summary>
        [Fact]
        public void ValidationException_WithMessage_ShouldSetMessage()
        {
            // Arrange
            string message = "Validation failed";

            // Act
            var exception = new ValidationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        /// <summary>
        /// Tests that ValidationException can be created with a message and validation errors.
        /// </summary>
        [Fact]
        public void ValidationException_WithMessageAndErrors_ShouldSetMessageAndErrors()
        {
            // Arrange
            string message = "Validation failed";
            var errors = new Dictionary<string, string[]>
            {
                { "Field1", new[] { "Error 1", "Error 2" } },
                { "Field2", new[] { "Error 3" } }
            };

            // Act
            var exception = new ValidationException(message, errors);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errors, exception.Errors);
        }

        /// <summary>
        /// Tests that ValidationException can be created with validation errors only.
        /// </summary>
        [Fact]
        public void ValidationException_WithErrorsOnly_ShouldSetDefaultMessageAndErrors()
        {
            // Arrange
            var errors = new Dictionary<string, string[]>
            {
                { "Field1", new[] { "Error 1", "Error 2" } },
                { "Field2", new[] { "Error 3" } }
            };

            // Act
            var exception = new ValidationException(errors);

            // Assert
            Assert.Contains("validation", exception.Message.ToLower());
            Assert.Equal(errors, exception.Errors);
        }
    }
}