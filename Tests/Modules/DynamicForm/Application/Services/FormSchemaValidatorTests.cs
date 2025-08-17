using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CoreAxis.Tests.Modules.DynamicForm.Application.Services
{
    public class FormSchemaValidatorTests
    {
        private readonly Mock<ILogger<FormSchemaValidator>> _loggerMock;
        private readonly FormSchemaValidator _validator;

        public FormSchemaValidatorTests()
        {
            _loggerMock = new Mock<ILogger<FormSchemaValidator>>();
            _validator = new FormSchemaValidator(_loggerMock.Object);
        }

        [Fact]
        public async Task ValidateAsync_WithValidSchema_ReturnsSuccess()
        {
            // Arrange
            var schema = CreateValidSchema();

            // Act
            var result = await _validator.ValidateAsync(schema);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value);
        }

        [Fact]
        public async Task ValidateAsync_WithNullSchema_ReturnsFailure()
        {
            // Act
            var result = await _validator.ValidateAsync(null);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Schema cannot be null", result.Error);
        }

        [Fact]
        public async Task ValidateAsync_WithEmptyTitle_ReturnsFailure()
        {
            // Arrange
            var schema = CreateValidSchema();
            schema = schema with { Title = "" };

            // Act
            var result = await _validator.ValidateAsync(schema);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Schema title is required", result.Error);
        }

        [Fact]
        public async Task ValidateAsync_WithUnsupportedVersion_ReturnsFailure()
        {
            // Arrange
            var schema = CreateValidSchema();
            schema = schema with { Version = "2.0" };

            // Act
            var result = await _validator.ValidateAsync(schema);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Unsupported schema version", result.Error);
        }

        [Fact]
        public async Task ValidateFieldsAsync_WithDuplicateFieldIds_ReturnsErrors()
        {
            // Arrange
            var fields = new List<FieldDefinition>
            {
                CreateValidField("field1", "Field 1"),
                CreateValidField("field1", "Field 2") // Duplicate ID
            };

            // Act
            var errors = await _validator.ValidateFieldsAsync(fields);

            // Assert
            Assert.Contains(errors, e => e.Contains("Duplicate field ID: field1"));
        }

        [Fact]
        public async Task ValidateFieldsAsync_WithSelectFieldWithoutOptions_ReturnsErrors()
        {
            // Arrange
            var field = CreateValidField("field1", "Field 1");
            field = field with { Type = "select", Options = new List<FieldOption>() };
            var fields = new List<FieldDefinition> { field };

            // Act
            var errors = await _validator.ValidateFieldsAsync(fields);

            // Assert
            Assert.Contains(errors, e => e.Contains("must have options"));
        }

        [Fact]
        public async Task ValidateConditionalLogicAsync_WithNonExistentTargetField_ReturnsErrors()
        {
            // Arrange
            var schema = CreateValidSchema();
            var conditionalLogic = new List<ConditionalLogic>
            {
                new ConditionalLogic
                {
                    TargetFieldId = "nonexistent",
                    Conditions = new List<Condition>
                    {
                        new Condition { FieldId = "field1", Operator = "equals", Value = "test" }
                    },
                    Action = "show"
                }
            };
            schema = schema with { ConditionalLogic = conditionalLogic };

            // Act
            var errors = await _validator.ValidateConditionalLogicAsync(schema);

            // Assert
            Assert.Contains(errors, e => e.Contains("references non-existent target field"));
        }

        [Fact]
        public async Task ValidateFormulasAsync_WithNonExistentTargetField_ReturnsErrors()
        {
            // Arrange
            var schema = CreateValidSchema();
            var formulas = new List<Formula>
            {
                new Formula
                {
                    TargetFieldId = "nonexistent",
                    Expression = "{field1} + 10"
                }
            };
            schema = schema with { Formulas = formulas };

            // Act
            var errors = await _validator.ValidateFormulasAsync(schema);

            // Assert
            Assert.Contains(errors, e => e.Contains("references non-existent target field"));
        }

        [Fact]
        public async Task ValidateFormulasAsync_WithEmptyExpression_ReturnsErrors()
        {
            // Arrange
            var schema = CreateValidSchema();
            var formulas = new List<Formula>
            {
                new Formula
                {
                    TargetFieldId = "field1",
                    Expression = ""
                }
            };
            schema = schema with { Formulas = formulas };

            // Act
            var errors = await _validator.ValidateFormulasAsync(schema);

            // Assert
            Assert.Contains(errors, e => e.Contains("Formula expression cannot be empty"));
        }

        [Fact]
        public async Task ValidateVersionCompatibilityAsync_WithCompatibleVersions_ReturnsSuccess()
        {
            // Arrange
            var schema = CreateValidSchema();
            schema = schema with { Version = "1.0" };

            // Act
            var result = await _validator.ValidateVersionCompatibilityAsync(schema, "1.1");

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ValidateVersionCompatibilityAsync_WithIncompatibleVersions_ReturnsFailure()
        {
            // Arrange
            var schema = CreateValidSchema();
            schema = schema with { Version = "1.2" };

            // Act
            var result = await _validator.ValidateVersionCompatibilityAsync(schema, "1.0");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("not compatible", result.Error);
        }

        [Fact]
        public async Task ValidateJsonAsync_WithInvalidJson_ReturnsFailure()
        {
            // Arrange
            var invalidJson = "{ invalid json }";

            // Act
            var result = await _validator.ValidateJsonAsync(invalidJson);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid JSON format", result.Error);
        }

        private FormSchema CreateValidSchema()
        {
            return new FormSchema
            {
                Version = "1.0",
                Title = "Test Form",
                Description = "Test Description",
                Fields = new List<FieldDefinition>
                {
                    CreateValidField("field1", "Field 1"),
                    CreateValidField("field2", "Field 2")
                },
                Configuration = new FormConfiguration(),
                ValidationRules = new List<ValidationRule>(),
                ConditionalLogic = new List<ConditionalLogic>(),
                Formulas = new List<Formula>(),
                Sections = new List<FormSection>(),
                Steps = new List<FormStep>(),
                Layout = new FormLayout(),
                Styling = new FormStyling(),
                Localizations = new List<FormLocalization>(),
                Metadata = new FormMetadata()
            };
        }

        private FieldDefinition CreateValidField(string id, string label)
        {
            return new FieldDefinition
            {
                Id = id,
                Name = id,
                Label = label,
                Type = "text",
                IsRequired = false,
                ValidationRules = new List<ValidationRule>(),
                Options = new List<FieldOption>(),
                DefaultValue = null,
                Placeholder = "",
                HelpText = "",
                IsVisible = true,
                IsReadOnly = false,
                Order = 1,
                GroupId = null,
                Attributes = new Dictionary<string, object>()
            };
        }
    }
}