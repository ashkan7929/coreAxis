using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SystemValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace CoreAxis.Modules.DynamicForm.Application.Services;

/// <summary>
/// Validation engine that supports expressions and internationalization
/// </summary>
public class ValidationEngine : IValidationEngine
{
    private readonly ILogger<ValidationEngine> _logger;
    private readonly Dictionary<string, Func<object?, Dictionary<string, object?>, Task<bool>>> _customRules;
    private readonly Dictionary<string, string> _defaultErrorMessages;

    public ValidationEngine(ILogger<ValidationEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _customRules = new Dictionary<string, Func<object?, Dictionary<string, object?>, Task<bool>>>();
        _defaultErrorMessages = InitializeDefaultErrorMessages();
    }

    /// <summary>
    /// Validates form data against field definitions
    /// </summary>
    public async Task<ValidationResult> ValidateAsync(
        Dictionary<string, object?> formData,
        List<FieldDefinition> fieldDefinitions,
        CultureInfo? culture = null)
    {
        if (formData == null)
            throw new ArgumentNullException(nameof(formData));
        if (fieldDefinitions == null)
            throw new ArgumentNullException(nameof(fieldDefinitions));

        var startTime = DateTime.UtcNow;
        var result = new ValidationResult
        {
            Metrics = new ValidationMetrics { StartTime = startTime }
        };

        try
        {
            _logger.LogDebug("Starting form validation for {FieldCount} fields", fieldDefinitions.Count);

            // Validate conditional rules first
            result.ConditionalResult = await ValidateConditionsAsync(formData, fieldDefinitions);

            // Validate each field
            var fieldTasks = fieldDefinitions.Select(async fieldDef =>
            {
                var fieldName = fieldDef.Name;
                var fieldValue = formData.TryGetValue(fieldName, out var value) ? value : null;
                
                var fieldResult = await ValidateFieldAsync(fieldName, fieldValue, fieldDef, formData, culture);
                return new { FieldName = fieldName, Result = fieldResult };
            });

            var fieldResults = await Task.WhenAll(fieldTasks);

            foreach (var fieldResult in fieldResults)
            {
                result.FieldResults[fieldResult.FieldName] = fieldResult.Result;
                result.Metrics.FieldsValidated++;
            }

            // Validate form-level rules
            await ValidateFormLevelRules(formData, fieldDefinitions, result, culture);

            // Determine overall validity
            result.IsValid = result.FieldResults.Values.All(fr => fr.IsValid) && !result.FormErrors.Any();

            var endTime = DateTime.UtcNow;
            result.Metrics.EndTime = endTime;
            result.Metrics.TotalTime = endTime - startTime;

            _logger.LogDebug("Form validation completed. IsValid: {IsValid}, Duration: {Duration}ms", 
                result.IsValid, result.Metrics.TotalTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during form validation");
            result.FormErrors.Add(new ValidationError
            {
                Code = "VALIDATION_ERROR",
                Message = GetLocalizedMessage("An error occurred during validation", culture),
                Severity = ValidationSeverity.Critical
            });
            result.IsValid = false;
            return result;
        }
    }

    /// <summary>
    /// Validates a single field value
    /// </summary>
    public async Task<FieldValidationResult> ValidateFieldAsync(
        string fieldName,
        object? value,
        FieldDefinition fieldDefinition,
        Dictionary<string, object?> formData,
        CultureInfo? culture = null)
    {
        if (string.IsNullOrEmpty(fieldName))
            throw new ArgumentException("Field name cannot be null or empty", nameof(fieldName));
        if (fieldDefinition == null)
            throw new ArgumentNullException(nameof(fieldDefinition));
        if (formData == null)
            throw new ArgumentNullException(nameof(formData));

        var result = new FieldValidationResult { SanitizedValue = value };

        try
        {
            // Check if field is visible and enabled
            var conditionalResult = await ValidateConditionsAsync(formData, new List<FieldDefinition> { fieldDefinition });
            
            if (!conditionalResult.VisibleFields.Contains(fieldName))
            {
                // Field is not visible, skip validation
                result.IsValid = true;
                return result;
            }

            // Sanitize value
            result.SanitizedValue = SanitizeValue(value, fieldDefinition.Type);

            // Check required validation
            var isRequired = fieldDefinition.IsRequired || conditionalResult.RequiredFields.Contains(fieldName);
            if (isRequired && IsValueEmpty(result.SanitizedValue))
            {
                result.Errors.Add(new ValidationError
                {
                    Code = "REQUIRED",
                    Message = GetLocalizedMessage($"The field '{fieldDefinition.Label}' is required", culture),
                    FieldName = fieldName,
                    Severity = ValidationSeverity.Error
                });
            }

            // Skip other validations if value is empty and not required
            if (!isRequired && IsValueEmpty(result.SanitizedValue))
            {
                result.IsValid = true;
                return result;
            }

            // Validate field type
            await ValidateFieldType(result, fieldDefinition, culture);

            // Validate custom rules
            await ValidateCustomRules(result, fieldDefinition, formData, culture);

            result.IsValid = !result.Errors.Any();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating field {FieldName}", fieldName);
            result.Errors.Add(new ValidationError
            {
                Code = "FIELD_VALIDATION_ERROR",
                Message = GetLocalizedMessage("An error occurred while validating this field", culture),
                FieldName = fieldName,
                Severity = ValidationSeverity.Critical
            });
            result.IsValid = false;
            return result;
        }
    }

    /// <summary>
    /// Validates conditional rules (show/hide, enable/disable)
    /// </summary>
    public async Task<ConditionalValidationResult> ValidateConditionsAsync(
        Dictionary<string, object?> formData,
        List<FieldDefinition> fieldDefinitions)
    {
        if (formData == null)
            throw new ArgumentNullException(nameof(formData));
        if (fieldDefinitions == null)
            throw new ArgumentNullException(nameof(fieldDefinitions));

        var result = new ConditionalValidationResult();

        try
        {
            foreach (var fieldDef in fieldDefinitions)
            {
                // Evaluate visibility
                if (string.IsNullOrEmpty(fieldDef.VisibilityExpression) || 
                    await EvaluateBooleanExpression(fieldDef.VisibilityExpression, formData))
                {
                    result.VisibleFields.Add(fieldDef.Name);
                }

                // Evaluate enabled state
                if (string.IsNullOrEmpty(fieldDef.EnabledExpression) || 
                    await EvaluateBooleanExpression(fieldDef.EnabledExpression, formData))
                {
                    result.EnabledFields.Add(fieldDef.Name);
                }

                // Evaluate required state
                if (!string.IsNullOrEmpty(fieldDef.RequiredExpression) && 
                    await EvaluateBooleanExpression(fieldDef.RequiredExpression, formData))
                {
                    result.RequiredFields.Add(fieldDef.Name);
                }

                // Evaluate dynamic options
                if (!string.IsNullOrEmpty(fieldDef.DynamicOptionsExpression))
                {
                    var options = await EvaluateDynamicOptions(fieldDef.DynamicOptionsExpression, formData);
                    if (options != null)
                    {
                        result.DynamicOptions[fieldDef.Name] = options;
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating conditional rules");
            // Return default state (all fields visible and enabled)
            foreach (var fieldDef in fieldDefinitions)
            {
                result.VisibleFields.Add(fieldDef.Name);
                result.EnabledFields.Add(fieldDef.Name);
            }
            return result;
        }
    }

    /// <summary>
    /// Registers a custom validation rule
    /// </summary>
    public void RegisterCustomRule(string ruleName, Func<object?, Dictionary<string, object?>, Task<bool>> validator)
    {
        if (string.IsNullOrEmpty(ruleName))
            throw new ArgumentException("Rule name cannot be null or empty", nameof(ruleName));
        if (validator == null)
            throw new ArgumentNullException(nameof(validator));

        _customRules[ruleName] = validator;
        _logger.LogDebug("Registered custom validation rule: {RuleName}", ruleName);
    }

    /// <summary>
    /// Gets supported validation rule types
    /// </summary>
    public IEnumerable<string> GetSupportedRuleTypes()
    {
        var builtInRules = new[]
        {
            "required", "minLength", "maxLength", "min", "max", "pattern", "email", "url", "phone",
            "date", "time", "datetime", "number", "integer", "decimal", "boolean", "custom"
        };

        return builtInRules.Concat(_customRules.Keys).Distinct();
    }

    #region Private Methods

    private Dictionary<string, string> InitializeDefaultErrorMessages()
    {
        return new Dictionary<string, string>
        {
            { "REQUIRED", "This field is required" },
            { "MIN_LENGTH", "This field must be at least {0} characters long" },
            { "MAX_LENGTH", "This field cannot exceed {0} characters" },
            { "MIN_VALUE", "This field must be at least {0}" },
            { "MAX_VALUE", "This field cannot exceed {0}" },
            { "PATTERN", "This field has an invalid format" },
            { "EMAIL", "Please enter a valid email address" },
            { "URL", "Please enter a valid URL" },
            { "PHONE", "Please enter a valid phone number" },
            { "DATE", "Please enter a valid date" },
            { "TIME", "Please enter a valid time" },
            { "DATETIME", "Please enter a valid date and time" },
            { "NUMBER", "Please enter a valid number" },
            { "INTEGER", "Please enter a valid integer" },
            { "DECIMAL", "Please enter a valid decimal number" },
            { "BOOLEAN", "Please enter a valid boolean value" }
        };
    }

    private async Task ValidateFieldType(FieldValidationResult result, FieldDefinition fieldDefinition, CultureInfo? culture)
    {
        var value = result.SanitizedValue;
        var fieldType = fieldDefinition.Type.ToLowerInvariant();

        switch (fieldType)
        {
            case "email":
                if (!IsValidEmail(value?.ToString()))
                {
                    result.Errors.Add(CreateValidationError("EMAIL", fieldDefinition.Name, culture));
                }
                break;

            case "url":
                if (!IsValidUrl(value?.ToString()))
                {
                    result.Errors.Add(CreateValidationError("URL", fieldDefinition.Name, culture));
                }
                break;

            case "phone":
                if (!IsValidPhone(value?.ToString()))
                {
                    result.Errors.Add(CreateValidationError("PHONE", fieldDefinition.Name, culture));
                }
                break;

            case "number":
            case "decimal":
                if (!IsValidNumber(value))
                {
                    result.Errors.Add(CreateValidationError("NUMBER", fieldDefinition.Name, culture));
                }
                break;

            case "integer":
                if (!IsValidInteger(value))
                {
                    result.Errors.Add(CreateValidationError("INTEGER", fieldDefinition.Name, culture));
                }
                break;

            case "date":
                if (!IsValidDate(value))
                {
                    result.Errors.Add(CreateValidationError("DATE", fieldDefinition.Name, culture));
                }
                break;

            case "time":
                if (!IsValidTime(value))
                {
                    result.Errors.Add(CreateValidationError("TIME", fieldDefinition.Name, culture));
                }
                break;

            case "datetime":
                if (!IsValidDateTime(value))
                {
                    result.Errors.Add(CreateValidationError("DATETIME", fieldDefinition.Name, culture));
                }
                break;

            case "boolean":
                if (!IsValidBoolean(value))
                {
                    result.Errors.Add(CreateValidationError("BOOLEAN", fieldDefinition.Name, culture));
                }
                break;
        }

        // Validate custom rules from field definition
        foreach (var rule in fieldDefinition.ValidationRules)
        {
            await ValidateRule(result, rule, fieldDefinition, culture);
        }
    }

    private async Task ValidateRule(FieldValidationResult result, ValidationRule rule, FieldDefinition fieldDefinition, CultureInfo? culture)
    {
        var value = result.SanitizedValue;
        var ruleType = rule.Type.ToLowerInvariant();

        switch (ruleType)
        {
            case "minlength":
                if (rule.Parameters.TryGetValue("value", out var minLengthObj) && 
                    int.TryParse(minLengthObj?.ToString(), out var minLength))
                {
                    if ((value?.ToString()?.Length ?? 0) < minLength)
                    {
                        result.Errors.Add(CreateValidationError("MIN_LENGTH", fieldDefinition.Name, culture, minLength));
                    }
                }
                break;

            case "maxlength":
                if (rule.Parameters.TryGetValue("value", out var maxLengthObj) && 
                    int.TryParse(maxLengthObj?.ToString(), out var maxLength))
                {
                    if ((value?.ToString()?.Length ?? 0) > maxLength)
                    {
                        result.Errors.Add(CreateValidationError("MAX_LENGTH", fieldDefinition.Name, culture, maxLength));
                    }
                }
                break;

            case "min":
                if (rule.Parameters.TryGetValue("value", out var minObj) && 
                    decimal.TryParse(minObj?.ToString(), out var minValue) &&
                    decimal.TryParse(value?.ToString(), out var currentValue))
                {
                    if (currentValue < minValue)
                    {
                        result.Errors.Add(CreateValidationError("MIN_VALUE", fieldDefinition.Name, culture, minValue));
                    }
                }
                break;

            case "max":
                if (rule.Parameters.TryGetValue("value", out var maxObj) && 
                    decimal.TryParse(maxObj?.ToString(), out var maxValue) &&
                    decimal.TryParse(value?.ToString(), out var currentVal))
                {
                    if (currentVal > maxValue)
                    {
                        result.Errors.Add(CreateValidationError("MAX_VALUE", fieldDefinition.Name, culture, maxValue));
                    }
                }
                break;

            case "pattern":
                if (rule.Parameters.TryGetValue("value", out var patternObj))
                {
                    var pattern = patternObj?.ToString();
                    if (!string.IsNullOrEmpty(pattern) && !Regex.IsMatch(value?.ToString() ?? "", pattern))
                    {
                        result.Errors.Add(CreateValidationError("PATTERN", fieldDefinition.Name, culture));
                    }
                }
                break;

            case "custom":
                if (rule.Parameters.TryGetValue("ruleName", out var ruleNameObj))
                {
                    var ruleName = ruleNameObj?.ToString();
                    if (!string.IsNullOrEmpty(ruleName) && _customRules.TryGetValue(ruleName, out var customValidator))
                    {
                        // This would need form data context - simplified for now
                        var isValid = await customValidator(value, new Dictionary<string, object?>());
                        if (!isValid)
                        {
                            result.Errors.Add(new ValidationError
                            {
                                Code = "CUSTOM_RULE",
                                Message = rule.ErrorMessage ?? GetLocalizedMessage("Custom validation failed", culture),
                                FieldName = fieldDefinition.Name,
                                Severity = ValidationSeverity.Error
                            });
                        }
                    }
                }
                break;
        }
    }

    private async Task ValidateCustomRules(FieldValidationResult result, FieldDefinition fieldDefinition, Dictionary<string, object?> formData, CultureInfo? culture)
    {
        // This method would integrate with ExpressionEngine for custom validation expressions
        // For now, it's a placeholder for future implementation
        await Task.CompletedTask;
    }

    private async Task ValidateFormLevelRules(Dictionary<string, object?> formData, List<FieldDefinition> fieldDefinitions, ValidationResult result, CultureInfo? culture)
    {
        // This method would validate form-level rules like cross-field validations
        // For now, it's a placeholder for future implementation
        await Task.CompletedTask;
    }

    private async Task<bool> EvaluateBooleanExpression(string expression, Dictionary<string, object?> formData)
    {
        // This would integrate with ExpressionEngine
        // For now, return true as default
        await Task.CompletedTask;
        return true;
    }

    private async Task<List<FieldOption>?> EvaluateDynamicOptions(string expression, Dictionary<string, object?> formData)
    {
        // This would integrate with ExpressionEngine to evaluate dynamic options
        // For now, return null
        await Task.CompletedTask;
        return null;
    }

    private object? SanitizeValue(object? value, string fieldType)
    {
        if (value == null) return null;

        var stringValue = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(stringValue)) return null;

        return fieldType.ToLowerInvariant() switch
        {
            "number" or "decimal" => decimal.TryParse(stringValue, out var decimalVal) ? decimalVal : value,
            "integer" => int.TryParse(stringValue, out var intVal) ? intVal : value,
            "boolean" => bool.TryParse(stringValue, out var boolVal) ? boolVal : value,
            "date" => DateTime.TryParse(stringValue, out var dateVal) ? dateVal : value,
            _ => stringValue
        };
    }

    private bool IsValueEmpty(object? value)
    {
        return value == null || 
               (value is string str && string.IsNullOrWhiteSpace(str)) ||
               (value is Array arr && arr.Length == 0);
    }

    private bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return new EmailAddressAttribute().IsValid(email);
    }

    private bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    private bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        return new PhoneAttribute().IsValid(phone);
    }

    private bool IsValidNumber(object? value)
    {
        return decimal.TryParse(value?.ToString(), out _);
    }

    private bool IsValidInteger(object? value)
    {
        return int.TryParse(value?.ToString(), out _);
    }

    private bool IsValidDate(object? value)
    {
        return DateTime.TryParse(value?.ToString(), out _);
    }

    private bool IsValidTime(object? value)
    {
        return TimeSpan.TryParse(value?.ToString(), out _);
    }

    private bool IsValidDateTime(object? value)
    {
        return DateTime.TryParse(value?.ToString(), out _);
    }

    private bool IsValidBoolean(object? value)
    {
        return bool.TryParse(value?.ToString(), out _);
    }

    private ValidationError CreateValidationError(string code, string fieldName, CultureInfo? culture, params object[] args)
    {
        var message = GetLocalizedMessage(_defaultErrorMessages.GetValueOrDefault(code, "Validation failed"), culture);
        if (args.Length > 0)
        {
            message = string.Format(message, args);
        }

        return new ValidationError
        {
            Code = code,
            Message = message,
            FieldName = fieldName,
            Severity = ValidationSeverity.Error
        };
    }

    private string GetLocalizedMessage(string message, CultureInfo? culture)
    {
        // This would integrate with a localization service
        // For now, return the original message
        return message;
    }

    #endregion
}