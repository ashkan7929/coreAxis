using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

namespace CoreAxis.Modules.DynamicForm.Domain.Interfaces;

/// <summary>
/// Interface for validation engine that supports expressions and internationalization
/// </summary>
public interface IValidationEngine
{
    /// <summary>
    /// Validates form data against field definitions
    /// </summary>
    /// <param name="formData">The form data to validate</param>
    /// <param name="fieldDefinitions">Field definitions containing validation rules</param>
    /// <param name="culture">Culture for localized error messages</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateAsync(
        Dictionary<string, object?> formData,
        List<FieldDefinition> fieldDefinitions,
        CultureInfo? culture = null);

    /// <summary>
    /// Validates a single field value
    /// </summary>
    /// <param name="fieldName">Name of the field</param>
    /// <param name="value">Value to validate</param>
    /// <param name="fieldDefinition">Field definition containing validation rules</param>
    /// <param name="formData">Complete form data for context-dependent validations</param>
    /// <param name="culture">Culture for localized error messages</param>
    /// <returns>Field validation result</returns>
    Task<FieldValidationResult> ValidateFieldAsync(
        string fieldName,
        object? value,
        FieldDefinition fieldDefinition,
        Dictionary<string, object?> formData,
        CultureInfo? culture = null);

    /// <summary>
    /// Validates conditional rules (show/hide, enable/disable)
    /// </summary>
    /// <param name="formData">The form data</param>
    /// <param name="fieldDefinitions">Field definitions containing conditional rules</param>
    /// <returns>Conditional validation result</returns>
    Task<ConditionalValidationResult> ValidateConditionsAsync(
        Dictionary<string, object?> formData,
        List<FieldDefinition> fieldDefinitions);

    /// <summary>
    /// Registers a custom validation rule
    /// </summary>
    /// <param name="ruleName">Name of the validation rule</param>
    /// <param name="validator">Validation function</param>
    void RegisterCustomRule(string ruleName, Func<object?, Dictionary<string, object?>, Task<bool>> validator);

    /// <summary>
    /// Gets supported validation rule types
    /// </summary>
    /// <returns>List of supported rule types</returns>
    IEnumerable<string> GetSupportedRuleTypes();
}

/// <summary>
/// Represents the result of form validation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates if the validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Field-specific validation results
    /// </summary>
    public Dictionary<string, FieldValidationResult> FieldResults { get; set; } = new();

    /// <summary>
    /// Form-level validation errors
    /// </summary>
    public List<ValidationError> FormErrors { get; set; } = new();

    /// <summary>
    /// Conditional validation results
    /// </summary>
    public ConditionalValidationResult? ConditionalResult { get; set; }

    /// <summary>
    /// Validation metrics
    /// </summary>
    public ValidationMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Represents the result of field validation
/// </summary>
public class FieldValidationResult
{
    /// <summary>
    /// Indicates if the field validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Field validation errors
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Field validation warnings
    /// </summary>
    public List<ValidationWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Sanitized/normalized field value
    /// </summary>
    public object? SanitizedValue { get; set; }
}

/// <summary>
/// Represents the result of conditional validation
/// </summary>
public class ConditionalValidationResult
{
    /// <summary>
    /// Fields that should be visible
    /// </summary>
    public HashSet<string> VisibleFields { get; set; } = new();

    /// <summary>
    /// Fields that should be enabled
    /// </summary>
    public HashSet<string> EnabledFields { get; set; } = new();

    /// <summary>
    /// Fields that are required based on conditions
    /// </summary>
    public HashSet<string> RequiredFields { get; set; } = new();

    /// <summary>
    /// Dynamic field options based on conditions
    /// </summary>
    public Dictionary<string, List<FieldOption>> DynamicOptions { get; set; } = new();
}

/// <summary>
/// Represents a validation error
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Field name associated with the error
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Severity level of the error
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object?> Context { get; set; } = new();
}

/// <summary>
/// Represents a validation warning
/// </summary>
public class ValidationWarning
{
    /// <summary>
    /// Warning code for programmatic handling
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Field name associated with the warning
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object?> Context { get; set; } = new();
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Validation performance metrics
/// </summary>
public class ValidationMetrics
{
    /// <summary>
    /// Total validation time
    /// </summary>
    public TimeSpan TotalTime { get; set; }

    /// <summary>
    /// Number of fields validated
    /// </summary>
    public int FieldsValidated { get; set; }

    /// <summary>
    /// Number of expressions evaluated
    /// </summary>
    public int ExpressionsEvaluated { get; set; }

    /// <summary>
    /// Number of custom rules executed
    /// </summary>
    public int CustomRulesExecuted { get; set; }

    /// <summary>
    /// Validation start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Validation end time
    /// </summary>
    public DateTime EndTime { get; set; }
}

/// <summary>
/// Field definition for validation
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// Field name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Field type (text, number, email, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Field label for display
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the field is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Validation rules for the field
    /// </summary>
    public List<ValidationRule> ValidationRules { get; set; } = new();

    /// <summary>
    /// Conditional visibility expression
    /// </summary>
    public string? VisibilityExpression { get; set; }

    /// <summary>
    /// Conditional enabled expression
    /// </summary>
    public string? EnabledExpression { get; set; }

    /// <summary>
    /// Conditional required expression
    /// </summary>
    public string? RequiredExpression { get; set; }

    /// <summary>
    /// Field options for select/radio fields
    /// </summary>
    public List<FieldOption> Options { get; set; } = new();

    /// <summary>
    /// Dynamic options expression
    /// </summary>
    public string? DynamicOptionsExpression { get; set; }

    /// <summary>
    /// Default value expression
    /// </summary>
    public string? DefaultValueExpression { get; set; }

    /// <summary>
    /// Field metadata
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();
}

/// <summary>
/// Validation rule definition
/// </summary>
public class ValidationRule
{
    /// <summary>
    /// Rule type (required, minLength, maxLength, pattern, custom, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Rule parameters
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();

    /// <summary>
    /// Custom error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error message key for i18n
    /// </summary>
    public string? ErrorMessageKey { get; set; }

    /// <summary>
    /// Conditional expression for when this rule applies
    /// </summary>
    public string? ConditionExpression { get; set; }

    /// <summary>
    /// Custom validation expression
    /// </summary>
    public string? ValidationExpression { get; set; }
}

/// <summary>
/// Field option for select/radio fields
/// </summary>
public class FieldOption
{
    /// <summary>
    /// Option value
    /// </summary>
    public object Value { get; set; } = string.Empty;

    /// <summary>
    /// Option label for display
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Option description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if the option is disabled
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Option group
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Option metadata
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();
}