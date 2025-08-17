using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Domain.ValueObjects
{
    /// <summary>
    /// Represents a validation rule for form fields.
    /// </summary>
    public class ValidationRule : IEquatable<ValidationRule>
    {
        /// <summary>
        /// Gets the validation rule type.
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Gets the validation rule value.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Gets the error message for validation failure.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Gets the error message key for internationalization.
        /// </summary>
        public string ErrorMessageKey { get; private set; }

        /// <summary>
        /// Gets additional parameters for the validation rule.
        /// </summary>
        public Dictionary<string, object> Parameters { get; private set; }

        /// <summary>
        /// Gets whether the validation rule is enabled.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Gets the validation rule priority (lower values have higher priority).
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// Gets the conditional expression for when this rule should be applied.
        /// </summary>
        public string Condition { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ValidationRule class.
        /// </summary>
        /// <param name="type">The validation rule type.</param>
        /// <param name="value">The validation rule value.</param>
        /// <param name="errorMessage">The error message for validation failure.</param>
        /// <param name="errorMessageKey">The error message key for internationalization.</param>
        /// <param name="parameters">Additional parameters for the validation rule.</param>
        /// <param name="isEnabled">Whether the validation rule is enabled.</param>
        /// <param name="priority">The validation rule priority.</param>
        /// <param name="condition">The conditional expression for when this rule should be applied.</param>
        public ValidationRule(
            string type,
            object value = null,
            string errorMessage = null,
            string errorMessageKey = null,
            Dictionary<string, object> parameters = null,
            bool isEnabled = true,
            int priority = 0,
            string condition = null)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Validation rule type cannot be null or empty.", nameof(type));

            Type = type.Trim();
            Value = value;
            ErrorMessage = errorMessage?.Trim();
            ErrorMessageKey = errorMessageKey?.Trim();
            Parameters = parameters ?? new Dictionary<string, object>();
            IsEnabled = isEnabled;
            Priority = priority;
            Condition = condition?.Trim();
        }

        /// <summary>
        /// Creates a required field validation rule.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorMessageKey">The error message key.</param>
        /// <param name="condition">The conditional expression.</param>
        /// <returns>A required validation rule.</returns>
        public static ValidationRule Required(string errorMessage = null, string errorMessageKey = null, string condition = null)
        {
            return new ValidationRule(
                "required",
                true,
                errorMessage ?? "This field is required.",
                errorMessageKey ?? "validation.required",
                condition: condition);
        }

        /// <summary>
        /// Creates a minimum length validation rule.
        /// </summary>
        /// <param name="minLength">The minimum length.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorMessageKey">The error message key.</param>
        /// <param name="condition">The conditional expression.</param>
        /// <returns>A minimum length validation rule.</returns>
        public static ValidationRule MinLength(int minLength, string errorMessage = null, string errorMessageKey = null, string condition = null)
        {
            if (minLength < 0)
                throw new ArgumentException("Minimum length cannot be negative.", nameof(minLength));

            return new ValidationRule(
                "minLength",
                minLength,
                errorMessage ?? $"Minimum length is {minLength} characters.",
                errorMessageKey ?? "validation.minLength",
                new Dictionary<string, object> { { "minLength", minLength } },
                condition: condition);
        }

        /// <summary>
        /// Creates a maximum length validation rule.
        /// </summary>
        /// <param name="maxLength">The maximum length.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorMessageKey">The error message key.</param>
        /// <param name="condition">The conditional expression.</param>
        /// <returns>A maximum length validation rule.</returns>
        public static ValidationRule MaxLength(int maxLength, string errorMessage = null, string errorMessageKey = null, string condition = null)
        {
            if (maxLength < 0)
                throw new ArgumentException("Maximum length cannot be negative.", nameof(maxLength));

            return new ValidationRule(
                "maxLength",
                maxLength,
                errorMessage ?? $"Maximum length is {maxLength} characters.",
                errorMessageKey ?? "validation.maxLength",
                new Dictionary<string, object> { { "maxLength", maxLength } },
                condition: condition);
        }

        /// <summary>
        /// Creates a minimum value validation rule.
        /// </summary>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorMessageKey">The error message key.</param>
        /// <param name="condition">The conditional expression.</param>
        /// <returns>A minimum value validation rule.</returns>
        public static ValidationRule MinValue(decimal minValue, string errorMessage = null, string errorMessageKey = null, string condition = null)
        {
            return new ValidationRule(
                "minValue",
                minValue,
                errorMessage ?? $"Minimum value is {minValue}.",
                errorMessageKey ?? "validation.minValue",
                new Dictionary<string, object> { { "minValue", minValue } },
                condition: condition);
        }

        /// <summary>
        /// Creates a maximum value validation rule.
        /// </summary>
        /// <param name="maxValue">The maximum value.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorMessageKey">The error message key.</param>
        /// <param name="condition">The conditional expression.</param>
        /// <returns>A maximum value validation rule.</returns>
        public static ValidationRule MaxValue(decimal maxValue, string errorMessage = null, string errorMessageKey = null, string condition = null)
        {
            return new ValidationRule(
                "maxValue",
                maxValue,
                errorMessage ?? $"Maximum value is {maxValue}.",
                errorMessageKey ?? "validation.maxValue",
                new Dictionary<string, object> { { "maxValue", maxValue } },
                condition: condition);
        }

        /// <summary>
        /// Creates a regular expression validation rule.
        /// </summary>
        /// <param name="pattern">The regular expression pattern.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorMessageKey">The error message key.</param>
        /// <param name="condition">The conditional expression.</param>
        /// <returns>A regular expression validation rule.</returns>
        public static ValidationRule Pattern(string pattern, string errorMessage = null, string errorMessageKey = null, string condition = null)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));

            return new ValidationRule(
                "pattern",
                pattern,
                errorMessage ?? "Invalid format.",
                errorMessageKey ?? "validation.pattern",
                new Dictionary<string, object> { { "pattern", pattern } },
                condition: condition);
        }

        /// <summary>
        /// Creates an email validation rule.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorMessageKey">The error message key.</param>
        /// <param name="condition">The conditional expression.</param>
        /// <returns>An email validation rule.</returns>
        public static ValidationRule Email(string errorMessage = null, string errorMessageKey = null, string condition = null)
        {
            return new ValidationRule(
                "email",
                true,
                errorMessage ?? "Please enter a valid email address.",
                errorMessageKey ?? "validation.email",
                condition: condition);
        }

        /// <summary>
        /// Creates a custom expression validation rule.
        /// </summary>
        /// <param name="expression">The validation expression.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorMessageKey">The error message key.</param>
        /// <param name="condition">The conditional expression.</param>
        /// <returns>A custom expression validation rule.</returns>
        public static ValidationRule Expression(string expression, string errorMessage = null, string errorMessageKey = null, string condition = null)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Expression cannot be null or empty.", nameof(expression));

            return new ValidationRule(
                "expression",
                expression,
                errorMessage ?? "Validation failed.",
                errorMessageKey ?? "validation.expression",
                new Dictionary<string, object> { { "expression", expression } },
                condition: condition);
        }

        /// <summary>
        /// Creates a copy of the validation rule with updated properties.
        /// </summary>
        /// <param name="isEnabled">The new enabled state.</param>
        /// <param name="priority">The new priority.</param>
        /// <param name="condition">The new condition.</param>
        /// <param name="errorMessage">The new error message.</param>
        /// <param name="errorMessageKey">The new error message key.</param>
        /// <returns>A new validation rule with updated properties.</returns>
        public ValidationRule WithUpdates(
            bool? isEnabled = null,
            int? priority = null,
            string condition = null,
            string errorMessage = null,
            string errorMessageKey = null)
        {
            return new ValidationRule(
                Type,
                Value,
                errorMessage ?? ErrorMessage,
                errorMessageKey ?? ErrorMessageKey,
                new Dictionary<string, object>(Parameters),
                isEnabled ?? IsEnabled,
                priority ?? Priority,
                condition ?? Condition);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(ValidationRule other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Type == other.Type &&
                   Equals(Value, other.Value) &&
                   ErrorMessage == other.ErrorMessage &&
                   ErrorMessageKey == other.ErrorMessageKey &&
                   IsEnabled == other.IsEnabled &&
                   Priority == other.Priority &&
                   Condition == other.Condition &&
                   Parameters.SequenceEqual(other.Parameters);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ValidationRule);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Type,
                Value,
                ErrorMessage,
                ErrorMessageKey,
                IsEnabled,
                Priority,
                Condition);
        }

        /// <summary>
        /// Returns a string representation of the validation rule.
        /// </summary>
        /// <returns>A string representation of the validation rule.</returns>
        public override string ToString()
        {
            var parts = new List<string> { $"Type: {Type}" };
            
            if (Value != null)
                parts.Add($"Value: {Value}");
            
            if (!string.IsNullOrEmpty(ErrorMessage))
                parts.Add($"Message: {ErrorMessage}");
            
            if (!IsEnabled)
                parts.Add("Disabled");
            
            if (Priority != 0)
                parts.Add($"Priority: {Priority}");
            
            if (!string.IsNullOrEmpty(Condition))
                parts.Add($"Condition: {Condition}");

            return $"ValidationRule({string.Join(", ", parts)})";
        }

        public static bool operator ==(ValidationRule left, ValidationRule right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ValidationRule left, ValidationRule right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Converts the validation rule to JSON string.
        /// </summary>
        /// <returns>JSON representation of the validation rule.</returns>
        public string ToJson()
        {
            var obj = new
            {
                type = Type,
                value = Value,
                errorMessage = ErrorMessage,
                errorMessageKey = ErrorMessageKey,
                parameters = Parameters,
                isEnabled = IsEnabled,
                priority = Priority,
                condition = Condition
            };
            
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }
    }
}