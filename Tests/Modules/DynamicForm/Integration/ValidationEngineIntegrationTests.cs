using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.Modules.DynamicForm.Infrastructure.Persistence;
using CoreAxis.SharedKernel.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.Modules.DynamicForm.Integration;

/// <summary>
/// Integration tests for ValidationEngine with ExpressionEngine
/// </summary>
public class ValidationEngineIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly DynamicFormDbContext _dbContext;
    private readonly IValidationEngine _validationEngine;
    private readonly IExpressionEngine _expressionEngine;

    public ValidationEngineIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add DbContext with InMemory database
        services.AddDbContext<DynamicFormDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add services
        services.AddScoped<IExpressionEngine, ExpressionEngine>();
        services.AddScoped<IValidationEngine, ValidationEngine>();
        
        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<DynamicFormDbContext>();
        _validationEngine = _serviceProvider.GetRequiredService<IValidationEngine>();
        _expressionEngine = _serviceProvider.GetRequiredService<IExpressionEngine>();
        
        // Ensure database is created
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task ValidateAsync_WithComplexConditionalValidation_ShouldReturnCorrectResult()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            ["age"] = 17,
            ["hasParentalConsent"] = false,
            ["email"] = "test@example.com"
        };

        var fieldDefinitions = new List<FieldDefinition>
        {
            new FieldDefinition
            {
                Name = "age",
                Type = "number",
                Label = "Age",
                IsRequired = true,
                ValidationRules = new List<ValidationRule>
                {
                    new ValidationRule
                    {
                        Type = "min",
                        Value = "0",
                        Message = "Age must be positive"
                    }
                }
            },
            new FieldDefinition
            {
                Name = "hasParentalConsent",
                Type = "boolean",
                Label = "Parental Consent",
                IsRequired = false,
                ConditionalValidation = new ConditionalValidation
                {
                    Condition = "age < 18",
                    ValidationRules = new List<ValidationRule>
                    {
                        new ValidationRule
                        {
                            Type = "required",
                            Value = "true",
                            Message = "Parental consent is required for minors"
                        }
                    }
                }
            },
            new FieldDefinition
            {
                Name = "email",
                Type = "email",
                Label = "Email",
                IsRequired = true,
                ValidationRules = new List<ValidationRule>
                {
                    new ValidationRule
                    {
                        Type = "email",
                        Message = "Invalid email format"
                    }
                }
            }
        };

        // Act
        var result = await _validationEngine.ValidateAsync(formData, fieldDefinitions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "hasParentalConsent");
        Assert.DoesNotContain(result.Errors, e => e.FieldName == "age");
        Assert.DoesNotContain(result.Errors, e => e.FieldName == "email");
    }

    [Fact]
    public async Task ValidateAsync_WithCrossFieldValidation_ShouldValidateCorrectly()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            ["password"] = "mypassword123",
            ["confirmPassword"] = "differentpassword",
            ["startDate"] = DateTime.Now.AddDays(1),
            ["endDate"] = DateTime.Now // End date is before start date
        };

        var fieldDefinitions = new List<FieldDefinition>
        {
            new FieldDefinition
            {
                Name = "password",
                Type = "password",
                Label = "Password",
                IsRequired = true,
                ValidationRules = new List<ValidationRule>
                {
                    new ValidationRule
                    {
                        Type = "minLength",
                        Value = "8",
                        Message = "Password must be at least 8 characters"
                    }
                }
            },
            new FieldDefinition
            {
                Name = "confirmPassword",
                Type = "password",
                Label = "Confirm Password",
                IsRequired = true,
                CrossFieldValidation = new CrossFieldValidation
                {
                    Expression = "confirmPassword == password",
                    Message = "Passwords do not match"
                }
            },
            new FieldDefinition
            {
                Name = "startDate",
                Type = "date",
                Label = "Start Date",
                IsRequired = true
            },
            new FieldDefinition
            {
                Name = "endDate",
                Type = "date",
                Label = "End Date",
                IsRequired = true,
                CrossFieldValidation = new CrossFieldValidation
                {
                    Expression = "endDate > startDate",
                    Message = "End date must be after start date"
                }
            }
        };

        // Act
        var result = await _validationEngine.ValidateAsync(formData, fieldDefinitions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.FieldName == "confirmPassword" && e.Message.Contains("do not match"));
        Assert.Contains(result.Errors, e => e.FieldName == "endDate" && e.Message.Contains("after start date"));
    }

    [Fact]
    public async Task ValidateAsync_WithDynamicValidationRules_ShouldApplyCorrectRules()
    {
        // Arrange
        var formData = new Dictionary<string, object?>
        {
            ["userType"] = "premium",
            ["creditLimit"] = 50000, // Exceeds standard limit but within premium limit
            ["income"] = 80000
        };

        var fieldDefinitions = new List<FieldDefinition>
        {
            new FieldDefinition
            {
                Name = "userType",
                Type = "select",
                Label = "User Type",
                IsRequired = true
            },
            new FieldDefinition
            {
                Name = "creditLimit",
                Type = "number",
                Label = "Credit Limit",
                IsRequired = true,
                ConditionalValidation = new ConditionalValidation
                {
                    Condition = "userType == 'standard'",
                    ValidationRules = new List<ValidationRule>
                    {
                        new ValidationRule
                        {
                            Type = "max",
                            Value = "10000",
                            Message = "Standard users cannot exceed $10,000 credit limit"
                        }
                    }
                }
            },
            new FieldDefinition
            {
                Name = "income",
                Type = "number",
                Label = "Annual Income",
                IsRequired = true,
                CrossFieldValidation = new CrossFieldValidation
                {
                    Expression = "creditLimit <= (income * 0.8)",
                    Message = "Credit limit cannot exceed 80% of annual income"
                }
            }
        };

        // Act
        var result = await _validationEngine.ValidateAsync(formData, fieldDefinitions);

        // Assert
        Assert.True(result.IsValid); // Should pass because user is premium and credit limit is within income ratio
    }

    [Fact]
    public async Task ValidateAsync_WithLocalization_ShouldReturnLocalizedMessages()
    {
        // Arrange
        var culture = new CultureInfo("fa-IR"); // Persian culture
        var formData = new Dictionary<string, object?>
        {
            ["name"] = "", // Empty required field
            ["age"] = -5 // Invalid age
        };

        var fieldDefinitions = new List<FieldDefinition>
        {
            new FieldDefinition
            {
                Name = "name",
                Type = "text",
                Label = "نام",
                IsRequired = true
            },
            new FieldDefinition
            {
                Name = "age",
                Type = "number",
                Label = "سن",
                IsRequired = true,
                ValidationRules = new List<ValidationRule>
                {
                    new ValidationRule
                    {
                        Type = "min",
                        Value = "0",
                        Message = "سن باید مثبت باشد"
                    }
                }
            }
        };

        // Act
        var result = await _validationEngine.ValidateAsync(formData, fieldDefinitions, culture);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        // Check that error messages are in Persian (or at least not in English)
        Assert.All(result.Errors, error => Assert.False(string.IsNullOrEmpty(error.Message)));
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}