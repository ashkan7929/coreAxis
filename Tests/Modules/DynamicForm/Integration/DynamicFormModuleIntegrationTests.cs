using CoreAxis.Modules.ApiManager.Application.Services;
using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.Modules.DynamicForm.Infrastructure.Persistence;
using CoreAxis.SharedKernel.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CoreAxis.Tests.Modules.DynamicForm.Integration;

/// <summary>
/// Comprehensive integration tests for the entire DynamicForm module
/// </summary>
public class DynamicFormModuleIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly DynamicFormDbContext _dbContext;
    private readonly IFormSchemaValidator _formSchemaValidator;
    private readonly IValidationEngine _validationEngine;
    private readonly IExpressionEngine _expressionEngine;
    private readonly IDynamicOptionsManager _dynamicOptionsManager;
    private readonly IFormulaService _formulaService;
    private readonly IIncrementalRecalculationEngine _recalculationEngine;

    public DynamicFormModuleIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add DbContext with InMemory database
        services.AddDbContext<DynamicFormDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add memory cache
        services.AddMemoryCache();
        
        // Add mocked dependencies
        var mockApiManager = new Mock<IApiManager>();
        services.AddSingleton(mockApiManager.Object);
        
        // Add all DynamicForm services
        services.AddScoped<IExpressionEngine, ExpressionEngine>();
        services.AddScoped<IValidationEngine, ValidationEngine>();
        services.AddScoped<IFormSchemaValidator, FormSchemaValidator>();
        services.AddScoped<IDynamicOptionsManager, DynamicOptionsManager>();
        services.AddScoped<IFormulaService, FormulaService>();
        services.AddScoped<IIncrementalRecalculationEngine, IncrementalRecalculationEngine>();
        
        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<DynamicFormDbContext>();
        _formSchemaValidator = _serviceProvider.GetRequiredService<IFormSchemaValidator>();
        _validationEngine = _serviceProvider.GetRequiredService<IValidationEngine>();
        _expressionEngine = _serviceProvider.GetRequiredService<IExpressionEngine>();
        _dynamicOptionsManager = _serviceProvider.GetRequiredService<IDynamicOptionsManager>();
        _formulaService = _serviceProvider.GetRequiredService<IFormulaService>();
        _recalculationEngine = _serviceProvider.GetRequiredService<IIncrementalRecalculationEngine>();
        
        // Ensure database is created
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task CompleteFormWorkflow_WithDynamicOptionsAndValidation_ShouldWorkEndToEnd()
    {
        // Arrange - Create a complex form schema
        var formSchema = new FormSchema
        {
            Title = "Customer Registration Form",
            Description = "Complete customer registration with dynamic options and validation",
            Fields = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    Name = "customerType",
                    Type = "select",
                    Label = "Customer Type",
                    IsRequired = true,
                    Options = new List<FieldOption>
                    {
                        FieldOption.Create("individual", "Individual"),
                        FieldOption.Create("business", "Business")
                    }
                },
                new FieldDefinition
                {
                    Name = "businessName",
                    Type = "text",
                    Label = "Business Name",
                    IsRequired = false,
                    ConditionalValidation = new ConditionalValidation
                    {
                        Condition = "customerType == 'business'",
                        ValidationRules = new List<ValidationRule>
                        {
                            new ValidationRule
                            {
                                Type = "required",
                                Message = "Business name is required for business customers"
                            }
                        }
                    }
                },
                new FieldDefinition
                {
                    Name = "country",
                    Type = "select",
                    Label = "Country",
                    IsRequired = true,
                    DynamicOptions = "static([{\"value\": \"US\", \"label\": \"United States\"}, {\"value\": \"CA\", \"label\": \"Canada\"}])"
                },
                new FieldDefinition
                {
                    Name = "state",
                    Type = "select",
                    Label = "State/Province",
                    IsRequired = true,
                    DynamicOptions = "if(country == 'US', static([{\"value\": \"CA\", \"label\": \"California\"}, {\"value\": \"NY\", \"label\": \"New York\"}]), static([{\"value\": \"ON\", \"label\": \"Ontario\"}, {\"value\": \"BC\", \"label\": \"British Columbia\"}]))"
                },
                new FieldDefinition
                {
                    Name = "creditLimit",
                    Type = "number",
                    Label = "Credit Limit",
                    IsRequired = true,
                    Formula = "if(customerType == 'business', income * 0.5, income * 0.3)"
                },
                new FieldDefinition
                {
                    Name = "income",
                    Type = "number",
                    Label = "Annual Income",
                    IsRequired = true,
                    ValidationRules = new List<ValidationRule>
                    {
                        new ValidationRule
                        {
                            Type = "min",
                            Value = "0",
                            Message = "Income must be positive"
                        }
                    }
                }
            }
        };

        // Step 1: Validate form schema
        var schemaValidationResult = await _formSchemaValidator.ValidateAsync(formSchema);
        Assert.True(schemaValidationResult.IsValid, $"Schema validation failed: {string.Join(", ", schemaValidationResult.Errors.Select(e => e.Message))}");

        // Step 2: Test dynamic options for country field
        var countryOptionsResult = await _dynamicOptionsManager.EvaluateDynamicOptionsAsync(
            "static([{\"value\": \"US\", \"label\": \"United States\"}, {\"value\": \"CA\", \"label\": \"Canada\"}])",
            new Dictionary<string, object?>());
        Assert.True(countryOptionsResult.IsSuccess);
        Assert.Equal(2, countryOptionsResult.Value.Count);

        // Step 3: Test dynamic options for state field based on country selection
        var formDataWithCountry = new Dictionary<string, object?> { ["country"] = "US" };
        var stateOptionsResult = await _dynamicOptionsManager.EvaluateDynamicOptionsAsync(
            "if(country == 'US', static([{\"value\": \"CA\", \"label\": \"California\"}, {\"value\": \"NY\", \"label\": \"New York\"}]), static([{\"value\": \"ON\", \"label\": \"Ontario\"}, {\"value\": \"BC\", \"label\": \"British Columbia\"}]))",
            formDataWithCountry);
        Assert.True(stateOptionsResult.IsSuccess);
        Assert.Equal(2, stateOptionsResult.Value.Count);
        Assert.Equal("CA", stateOptionsResult.Value[0].Value);
        Assert.Equal("California", stateOptionsResult.Value[0].Label);

        // Step 4: Test form validation with business customer
        var businessFormData = new Dictionary<string, object?>
        {
            ["customerType"] = "business",
            ["businessName"] = "Acme Corp",
            ["country"] = "US",
            ["state"] = "CA",
            ["income"] = 100000,
            ["creditLimit"] = 50000
        };

        var businessValidationResult = await _validationEngine.ValidateAsync(businessFormData, formSchema.Fields);
        Assert.True(businessValidationResult.IsValid, $"Business validation failed: {string.Join(", ", businessValidationResult.Errors.Select(e => e.Message))}");

        // Step 5: Test form validation with individual customer (missing business name should be OK)
        var individualFormData = new Dictionary<string, object?>
        {
            ["customerType"] = "individual",
            ["country"] = "CA",
            ["state"] = "ON",
            ["income"] = 60000,
            ["creditLimit"] = 18000
        };

        var individualValidationResult = await _validationEngine.ValidateAsync(individualFormData, formSchema.Fields);
        Assert.True(individualValidationResult.IsValid, $"Individual validation failed: {string.Join(", ", individualValidationResult.Errors.Select(e => e.Message))}");

        // Step 6: Test formula calculation for credit limit
        var businessCreditFormula = "if(customerType == 'business', income * 0.5, income * 0.3)";
        var businessCreditResult = await _expressionEngine.EvaluateAsync(businessCreditFormula, businessFormData);
        Assert.True(businessCreditResult.IsSuccess);
        Assert.Equal(50000.0, Convert.ToDouble(businessCreditResult.Value)); // 100000 * 0.5

        var individualCreditResult = await _expressionEngine.EvaluateAsync(businessCreditFormula, individualFormData);
        Assert.True(individualCreditResult.IsSuccess);
        Assert.Equal(18000.0, Convert.ToDouble(individualCreditResult.Value)); // 60000 * 0.3
    }

    [Fact]
    public async Task FormWithDependentFields_ShouldRecalculateCorrectly()
    {
        // Arrange - Create form with dependent calculations
        var formData = new Dictionary<string, object?>
        {
            ["quantity"] = 10,
            ["unitPrice"] = 25.50,
            ["discountPercent"] = 10
        };

        var fieldDefinitions = new List<FieldDefinition>
        {
            new FieldDefinition
            {
                Name = "quantity",
                Type = "number",
                Label = "Quantity",
                IsRequired = true
            },
            new FieldDefinition
            {
                Name = "unitPrice",
                Type = "number",
                Label = "Unit Price",
                IsRequired = true
            },
            new FieldDefinition
            {
                Name = "discountPercent",
                Type = "number",
                Label = "Discount %",
                IsRequired = false
            },
            new FieldDefinition
            {
                Name = "subtotal",
                Type = "number",
                Label = "Subtotal",
                IsReadOnly = true,
                Formula = "quantity * unitPrice"
            },
            new FieldDefinition
            {
                Name = "discountAmount",
                Type = "number",
                Label = "Discount Amount",
                IsReadOnly = true,
                Formula = "subtotal * (discountPercent / 100)"
            },
            new FieldDefinition
            {
                Name = "total",
                Type = "number",
                Label = "Total",
                IsReadOnly = true,
                Formula = "subtotal - discountAmount"
            }
        };

        // Act - Calculate all dependent fields
        var calculationResult = await _recalculationEngine.RecalculateAsync(formData, fieldDefinitions);

        // Assert
        Assert.True(calculationResult.IsSuccess);
        Assert.NotNull(calculationResult.Value);
        
        var updatedData = calculationResult.Value;
        Assert.Equal(255.0, Convert.ToDouble(updatedData["subtotal"])); // 10 * 25.50
        Assert.Equal(25.5, Convert.ToDouble(updatedData["discountAmount"])); // 255 * 0.10
        Assert.Equal(229.5, Convert.ToDouble(updatedData["total"])); // 255 - 25.5
    }

    [Fact]
    public async Task FormValidation_WithInvalidBusinessCustomer_ShouldReturnErrors()
    {
        // Arrange - Business customer without business name
        var invalidFormData = new Dictionary<string, object?>
        {
            ["customerType"] = "business",
            // Missing businessName
            ["country"] = "US",
            ["state"] = "CA",
            ["income"] = -1000, // Invalid negative income
            ["creditLimit"] = 50000
        };

        var fieldDefinitions = new List<FieldDefinition>
        {
            new FieldDefinition
            {
                Name = "customerType",
                Type = "select",
                Label = "Customer Type",
                IsRequired = true
            },
            new FieldDefinition
            {
                Name = "businessName",
                Type = "text",
                Label = "Business Name",
                IsRequired = false,
                ConditionalValidation = new ConditionalValidation
                {
                    Condition = "customerType == 'business'",
                    ValidationRules = new List<ValidationRule>
                    {
                        new ValidationRule
                        {
                            Type = "required",
                            Message = "Business name is required for business customers"
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
                ValidationRules = new List<ValidationRule>
                {
                    new ValidationRule
                    {
                        Type = "min",
                        Value = "0",
                        Message = "Income must be positive"
                    }
                }
            }
        };

        // Act
        var validationResult = await _validationEngine.ValidateAsync(invalidFormData, fieldDefinitions);

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Equal(2, validationResult.Errors.Count);
        Assert.Contains(validationResult.Errors, e => e.FieldName == "businessName");
        Assert.Contains(validationResult.Errors, e => e.FieldName == "income");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}