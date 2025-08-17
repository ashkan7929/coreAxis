using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;

namespace CoreAxis.Tests.Modules.DynamicForm.Application.Services;

/// <summary>
/// Unit tests for ValidationEngine
/// </summary>
public class ValidationEngineTests
{
    private readonly Mock<ILogger<ValidationEngine>> _mockLogger;
    private readonly ValidationEngine _engine;

    public ValidationEngineTests()
    {
        _mockLogger = new Mock<ILogger<ValidationEngine>>();
        _engine = new ValidationEngine(_mockLogger.Object);
    }

    [Fact]
    public async Task ValidateAsync_ValidFormData_ShouldReturnValidResult()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            { "name", "John Doe" },
            { "email", "john@example.com" },
            { "age", 25 }
        };

        var fieldDefinitions = new List<FieldDefinition>
        {
            new FieldDefinition
            {
                Name = "name",
                Type = "text",
                Label = "Name",
                IsRequired = true
            },
            new FieldDefinition
            {
                Name = "email",
                Type = "email",
                Label = "Email",
                IsRequired = true
            },
            new FieldDefinition
            {
                Name = "age",
                Type = "integer",
                Label = "Age",
                IsRequired = false
            }
        };

        // Act
        var result = await _engine.ValidateAsync(formData, fieldDefinitions);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(3, result.FieldResults.Count);
        Assert.All(result.FieldResults.Values, fr => Assert.True(fr.IsValid));
        Assert.Empty(result.FormErrors);
    }

    [Fact]
    public async Task ValidateAsync_MissingRequiredField_ShouldReturnInvalidResult()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            { "email", "john@example.com" }
        };

        var fieldDefinitions = new List<FieldDefinition>
        {
            new FieldDefinition
            {
                Name = "name",
                Type = "text",
                Label = "Name",
                IsRequired = true
            },
            new FieldDefinition
            {
                Name = "email",
                Type = "email",
                Label = "Email",
                IsRequired = true
            }
        };

        // Act
        var result = await _engine.ValidateAsync(formData, fieldDefinitions);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.True(result.FieldResults.ContainsKey("name"));
        Assert.False(result.FieldResults["name"].IsValid);
        Assert.Contains(result.FieldResults["name"].Errors, e => e.Code == "REQUIRED");
    }

    [Fact]
    public async Task ValidateFieldAsync_ValidEmail_ShouldReturnValidResult()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "email",
            Type = "email",
            Label = "Email",
            IsRequired = true
        };

        var formData = new Dictionary<string, object?>
        {
            { "email", "john@example.com" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("email", "john@example.com", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal("john@example.com", result.SanitizedValue);
    }

    [Fact]
    public async Task ValidateFieldAsync_InvalidEmail_ShouldReturnInvalidResult()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "email",
            Type = "email",
            Label = "Email",
            IsRequired = true
        };

        var formData = new Dictionary<string, object?>
        {
            { "email", "invalid-email" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("email", "invalid-email", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "EMAIL");
    }

    [Fact]
    public async Task ValidateFieldAsync_MinLengthValidation_ShouldWork()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "password",
            Type = "text",
            Label = "Password",
            IsRequired = true,
            ValidationRules = new List<ValidationRule>
            {
                new ValidationRule
                {
                    Type = "minLength",
                    Parameters = new Dictionary<string, object?> { { "value", 8 } }
                }
            }
        };

        var formData = new Dictionary<string, object?>
        {
            { "password", "123" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("password", "123", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "MIN_LENGTH");
    }

    [Fact]
    public async Task ValidateFieldAsync_MaxLengthValidation_ShouldWork()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "description",
            Type = "text",
            Label = "Description",
            IsRequired = false,
            ValidationRules = new List<ValidationRule>
            {
                new ValidationRule
                {
                    Type = "maxLength",
                    Parameters = new Dictionary<string, object?> { { "value", 10 } }
                }
            }
        };

        var formData = new Dictionary<string, object?>
        {
            { "description", "This is a very long description that exceeds the limit" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("description", "This is a very long description that exceeds the limit", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "MAX_LENGTH");
    }

    [Fact]
    public async Task ValidateFieldAsync_NumberValidation_ShouldWork()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "price",
            Type = "number",
            Label = "Price",
            IsRequired = true
        };

        var formData = new Dictionary<string, object?>
        {
            { "price", "not-a-number" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("price", "not-a-number", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "NUMBER");
    }

    [Fact]
    public async Task ValidateFieldAsync_IntegerValidation_ShouldWork()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "quantity",
            Type = "integer",
            Label = "Quantity",
            IsRequired = true
        };

        var formData = new Dictionary<string, object?>
        {
            { "quantity", "3.14" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("quantity", "3.14", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "INTEGER");
    }

    [Fact]
    public async Task ValidateFieldAsync_MinValueValidation_ShouldWork()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "age",
            Type = "integer",
            Label = "Age",
            IsRequired = true,
            ValidationRules = new List<ValidationRule>
            {
                new ValidationRule
                {
                    Type = "min",
                    Parameters = new Dictionary<string, object?> { { "value", 18 } }
                }
            }
        };

        var formData = new Dictionary<string, object?>
        {
            { "age", "16" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("age", "16", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "MIN_VALUE");
    }

    [Fact]
    public async Task ValidateFieldAsync_MaxValueValidation_ShouldWork()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "score",
            Type = "integer",
            Label = "Score",
            IsRequired = true,
            ValidationRules = new List<ValidationRule>
            {
                new ValidationRule
                {
                    Type = "max",
                    Parameters = new Dictionary<string, object?> { { "value", 100 } }
                }
            }
        };

        var formData = new Dictionary<string, object?>
        {
            { "score", "150" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("score", "150", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "MAX_VALUE");
    }

    [Fact]
    public async Task ValidateFieldAsync_PatternValidation_ShouldWork()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "zipCode",
            Type = "text",
            Label = "Zip Code",
            IsRequired = true,
            ValidationRules = new List<ValidationRule>
            {
                new ValidationRule
                {
                    Type = "pattern",
                    Parameters = new Dictionary<string, object?> { { "value", @"^\d{5}$" } }
                }
            }
        };

        var formData = new Dictionary<string, object?>
        {
            { "zipCode", "1234" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("zipCode", "1234", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "PATTERN");
    }

    [Fact]
    public async Task ValidateFieldAsync_UrlValidation_ShouldWork()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "website",
            Type = "url",
            Label = "Website",
            IsRequired = false
        };

        var formData = new Dictionary<string, object?>
        {
            { "website", "not-a-url" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("website", "not-a-url", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "URL");
    }

    [Fact]
    public async Task ValidateFieldAsync_DateValidation_ShouldWork()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "birthDate",
            Type = "date",
            Label = "Birth Date",
            IsRequired = true
        };

        var formData = new Dictionary<string, object?>
        {
            { "birthDate", "invalid-date" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("birthDate", "invalid-date", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "DATE");
    }

    [Fact]
    public async Task ValidateFieldAsync_BooleanValidation_ShouldWork()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "isActive",
            Type = "boolean",
            Label = "Is Active",
            IsRequired = true
        };

        var formData = new Dictionary<string, object?>
        {
            { "isActive", "maybe" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("isActive", "maybe", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "BOOLEAN");
    }

    [Fact]
    public async Task ValidateConditionsAsync_ShouldReturnAllFieldsVisibleByDefault()
    {
        // Arrange
        var fieldDefinitions = new List<FieldDefinition>
        {
            new FieldDefinition { Name = "field1", Type = "text" },
            new FieldDefinition { Name = "field2", Type = "text" },
            new FieldDefinition { Name = "field3", Type = "text" }
        };

        var formData = new Dictionary<string, object?>();

        // Act
        var result = await _engine.ValidateConditionsAsync(formData, fieldDefinitions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.VisibleFields.Count);
        Assert.Equal(3, result.EnabledFields.Count);
        Assert.Contains("field1", result.VisibleFields);
        Assert.Contains("field2", result.VisibleFields);
        Assert.Contains("field3", result.VisibleFields);
    }

    [Fact]
    public void RegisterCustomRule_ValidRule_ShouldRegisterSuccessfully()
    {
        // Arrange
        var ruleName = "customRule";
        Func<object?, Dictionary<string, object?>, Task<bool>> validator = (value, formData) => Task.FromResult(true);

        // Act
        _engine.RegisterCustomRule(ruleName, validator);
        var supportedRules = _engine.GetSupportedRuleTypes();

        // Assert
        Assert.Contains(ruleName, supportedRules);
    }

    [Fact]
    public void RegisterCustomRule_NullRuleName_ShouldThrowArgumentException()
    {
        // Arrange
        Func<object?, Dictionary<string, object?>, Task<bool>> validator = (value, formData) => Task.FromResult(true);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _engine.RegisterCustomRule(null!, validator));
        Assert.Throws<ArgumentException>(() => _engine.RegisterCustomRule("", validator));
    }

    [Fact]
    public void RegisterCustomRule_NullValidator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _engine.RegisterCustomRule("test", null!));
    }

    [Fact]
    public void GetSupportedRuleTypes_ShouldReturnBuiltInRules()
    {
        // Act
        var supportedRules = _engine.GetSupportedRuleTypes().ToList();

        // Assert
        Assert.Contains("required", supportedRules);
        Assert.Contains("minLength", supportedRules);
        Assert.Contains("maxLength", supportedRules);
        Assert.Contains("min", supportedRules);
        Assert.Contains("max", supportedRules);
        Assert.Contains("pattern", supportedRules);
        Assert.Contains("email", supportedRules);
        Assert.Contains("url", supportedRules);
        Assert.Contains("phone", supportedRules);
        Assert.Contains("date", supportedRules);
        Assert.Contains("time", supportedRules);
        Assert.Contains("datetime", supportedRules);
        Assert.Contains("number", supportedRules);
        Assert.Contains("integer", supportedRules);
        Assert.Contains("decimal", supportedRules);
        Assert.Contains("boolean", supportedRules);
        Assert.Contains("custom", supportedRules);
    }

    [Fact]
    public async Task ValidateAsync_NullFormData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var fieldDefinitions = new List<FieldDefinition>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _engine.ValidateAsync(null!, fieldDefinitions));
    }

    [Fact]
    public async Task ValidateAsync_NullFieldDefinitions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var formData = new Dictionary<string, object?>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _engine.ValidateAsync(formData, null!));
    }

    [Fact]
    public async Task ValidateFieldAsync_NullFieldName_ShouldThrowArgumentException()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition { Name = "test", Type = "text" };
        var formData = new Dictionary<string, object?>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _engine.ValidateFieldAsync(null!, "value", fieldDefinition, formData));
        await Assert.ThrowsAsync<ArgumentException>(() => _engine.ValidateFieldAsync("", "value", fieldDefinition, formData));
    }

    [Fact]
    public async Task ValidateFieldAsync_NullFieldDefinition_ShouldThrowArgumentNullException()
    {
        // Arrange
        var formData = new Dictionary<string, object?>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _engine.ValidateFieldAsync("field", "value", null!, formData));
    }

    [Fact]
    public async Task ValidateFieldAsync_NullFormData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition { Name = "test", Type = "text" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _engine.ValidateFieldAsync("field", "value", fieldDefinition, null!));
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationEngine(null!));
    }

    [Fact]
    public async Task ValidateFieldAsync_OptionalFieldWithEmptyValue_ShouldBeValid()
    {
        // Arrange
        var fieldDefinition = new FieldDefinition
        {
            Name = "description",
            Type = "text",
            Label = "Description",
            IsRequired = false
        };

        var formData = new Dictionary<string, object?>
        {
            { "description", "" }
        };

        // Act
        var result = await _engine.ValidateFieldAsync("description", "", fieldDefinition, formData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithCulture_ShouldUseProvidedCulture()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            { "name", "" }
        };

        var fieldDefinitions = new List<FieldDefinition>
        {
            new FieldDefinition
            {
                Name = "name",
                Type = "text",
                Label = "Name",
                IsRequired = true
            }
        };

        var culture = new CultureInfo("en-US");

        // Act
        var result = await _engine.ValidateAsync(formData, fieldDefinitions, culture);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.True(result.FieldResults.ContainsKey("name"));
        Assert.False(result.FieldResults["name"].IsValid);
    }
}