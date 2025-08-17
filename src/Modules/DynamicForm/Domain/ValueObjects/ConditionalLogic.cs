using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Domain.ValueObjects
{
    /// <summary>
    /// Represents conditional logic for form fields that determines visibility, requirement, or other behaviors.
    /// </summary>
    public class ConditionalLogic : IEquatable<ConditionalLogic>
    {
        /// <summary>
        /// Gets the conditional expression that determines when the logic should be applied.
        /// </summary>
        public string Expression { get; private set; }

        /// <summary>
        /// Gets the action to perform when the condition is met.
        /// </summary>
        public ConditionalAction Action { get; private set; }

        /// <summary>
        /// Gets the target value or configuration for the action.
        /// </summary>
        public object TargetValue { get; private set; }

        /// <summary>
        /// Gets whether the logic is enabled.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Gets the priority of this conditional logic (lower values have higher priority).
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// Gets the description of this conditional logic for documentation purposes.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets additional parameters for the conditional logic.
        /// </summary>
        public Dictionary<string, object> Parameters { get; private set; }

        /// <summary>
        /// Gets the fields that this conditional logic depends on.
        /// </summary>
        public HashSet<string> DependentFields { get; private set; }

        /// <summary>
        /// Gets the evaluation mode for the conditional logic.
        /// </summary>
        public ConditionalEvaluationMode EvaluationMode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ConditionalLogic class.
        /// </summary>
        /// <param name="expression">The conditional expression.</param>
        /// <param name="action">The action to perform.</param>
        /// <param name="targetValue">The target value for the action.</param>
        /// <param name="isEnabled">Whether the logic is enabled.</param>
        /// <param name="priority">The priority of the logic.</param>
        /// <param name="description">The description of the logic.</param>
        /// <param name="parameters">Additional parameters.</param>
        /// <param name="dependentFields">The fields this logic depends on.</param>
        /// <param name="evaluationMode">The evaluation mode.</param>
        public ConditionalLogic(
            string expression,
            ConditionalAction action,
            object targetValue = null,
            bool isEnabled = true,
            int priority = 0,
            string description = null,
            Dictionary<string, object> parameters = null,
            HashSet<string> dependentFields = null,
            ConditionalEvaluationMode evaluationMode = ConditionalEvaluationMode.Immediate)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Expression cannot be null or empty.", nameof(expression));

            Expression = expression.Trim();
            Action = action;
            TargetValue = targetValue;
            IsEnabled = isEnabled;
            Priority = priority;
            Description = description?.Trim();
            Parameters = parameters ?? new Dictionary<string, object>();
            DependentFields = dependentFields ?? new HashSet<string>();
            EvaluationMode = evaluationMode;
        }

        /// <summary>
        /// Creates a show/hide conditional logic.
        /// </summary>
        /// <param name="expression">The conditional expression.</param>
        /// <param name="show">Whether to show (true) or hide (false) when condition is met.</param>
        /// <param name="dependentFields">The fields this logic depends on.</param>
        /// <param name="description">The description of the logic.</param>
        /// <returns>A new conditional logic for visibility control.</returns>
        public static ConditionalLogic ShowHide(string expression, bool show = true, HashSet<string> dependentFields = null, string description = null)
        {
            return new ConditionalLogic(
                expression,
                ConditionalAction.SetVisibility,
                show,
                description: description ?? (show ? "Show field when condition is met" : "Hide field when condition is met"),
                dependentFields: dependentFields);
        }

        /// <summary>
        /// Creates a required/optional conditional logic.
        /// </summary>
        /// <param name="expression">The conditional expression.</param>
        /// <param name="required">Whether to make required (true) or optional (false) when condition is met.</param>
        /// <param name="dependentFields">The fields this logic depends on.</param>
        /// <param name="description">The description of the logic.</param>
        /// <returns>A new conditional logic for requirement control.</returns>
        public static ConditionalLogic SetRequired(string expression, bool required = true, HashSet<string> dependentFields = null, string description = null)
        {
            return new ConditionalLogic(
                expression,
                ConditionalAction.SetRequired,
                required,
                description: description ?? (required ? "Make field required when condition is met" : "Make field optional when condition is met"),
                dependentFields: dependentFields);
        }

        /// <summary>
        /// Creates an enable/disable conditional logic.
        /// </summary>
        /// <param name="expression">The conditional expression.</param>
        /// <param name="enabled">Whether to enable (true) or disable (false) when condition is met.</param>
        /// <param name="dependentFields">The fields this logic depends on.</param>
        /// <param name="description">The description of the logic.</param>
        /// <returns>A new conditional logic for enabled state control.</returns>
        public static ConditionalLogic SetEnabled(string expression, bool enabled = true, HashSet<string> dependentFields = null, string description = null)
        {
            return new ConditionalLogic(
                expression,
                ConditionalAction.SetEnabled,
                enabled,
                description: description ?? (enabled ? "Enable field when condition is met" : "Disable field when condition is met"),
                dependentFields: dependentFields);
        }

        /// <summary>
        /// Creates a value setting conditional logic.
        /// </summary>
        /// <param name="expression">The conditional expression.</param>
        /// <param name="value">The value to set when condition is met.</param>
        /// <param name="dependentFields">The fields this logic depends on.</param>
        /// <param name="description">The description of the logic.</param>
        /// <returns>A new conditional logic for value setting.</returns>
        public static ConditionalLogic SetValue(string expression, object value, HashSet<string> dependentFields = null, string description = null)
        {
            return new ConditionalLogic(
                expression,
                ConditionalAction.SetValue,
                value,
                description: description ?? $"Set field value to '{value}' when condition is met",
                dependentFields: dependentFields);
        }

        /// <summary>
        /// Creates a validation rule conditional logic.
        /// </summary>
        /// <param name="expression">The conditional expression.</param>
        /// <param name="validationRules">The validation rules to apply when condition is met.</param>
        /// <param name="dependentFields">The fields this logic depends on.</param>
        /// <param name="description">The description of the logic.</param>
        /// <returns>A new conditional logic for validation control.</returns>
        public static ConditionalLogic SetValidation(string expression, IEnumerable<ValidationRule> validationRules, HashSet<string> dependentFields = null, string description = null)
        {
            return new ConditionalLogic(
                expression,
                ConditionalAction.SetValidation,
                validationRules?.ToList(),
                description: description ?? "Apply validation rules when condition is met",
                dependentFields: dependentFields);
        }

        /// <summary>
        /// Creates an options filtering conditional logic.
        /// </summary>
        /// <param name="expression">The conditional expression.</param>
        /// <param name="options">The options to show when condition is met.</param>
        /// <param name="dependentFields">The fields this logic depends on.</param>
        /// <param name="description">The description of the logic.</param>
        /// <returns>A new conditional logic for options filtering.</returns>
        public static ConditionalLogic FilterOptions(string expression, IEnumerable<FieldOption> options, HashSet<string> dependentFields = null, string description = null)
        {
            return new ConditionalLogic(
                expression,
                ConditionalAction.FilterOptions,
                options?.ToList(),
                description: description ?? "Filter available options when condition is met",
                dependentFields: dependentFields);
        }

        /// <summary>
        /// Creates a custom action conditional logic.
        /// </summary>
        /// <param name="expression">The conditional expression.</param>
        /// <param name="actionName">The custom action name.</param>
        /// <param name="parameters">The parameters for the custom action.</param>
        /// <param name="dependentFields">The fields this logic depends on.</param>
        /// <param name="description">The description of the logic.</param>
        /// <returns>A new conditional logic for custom actions.</returns>
        public static ConditionalLogic CustomAction(string expression, string actionName, Dictionary<string, object> parameters = null, HashSet<string> dependentFields = null, string description = null)
        {
            var allParameters = new Dictionary<string, object>(parameters ?? new Dictionary<string, object>())
            {
                ["actionName"] = actionName
            };

            return new ConditionalLogic(
                expression,
                ConditionalAction.Custom,
                actionName,
                description: description ?? $"Execute custom action '{actionName}' when condition is met",
                parameters: allParameters,
                dependentFields: dependentFields);
        }

        /// <summary>
        /// Creates a copy of the conditional logic with updated properties.
        /// </summary>
        /// <param name="expression">The new expression.</param>
        /// <param name="action">The new action.</param>
        /// <param name="targetValue">The new target value.</param>
        /// <param name="isEnabled">The new enabled state.</param>
        /// <param name="priority">The new priority.</param>
        /// <param name="description">The new description.</param>
        /// <param name="evaluationMode">The new evaluation mode.</param>
        /// <returns>A new conditional logic with updated properties.</returns>
        public ConditionalLogic WithUpdates(
            string expression = null,
            ConditionalAction? action = null,
            object targetValue = null,
            bool? isEnabled = null,
            int? priority = null,
            string description = null,
            ConditionalEvaluationMode? evaluationMode = null)
        {
            return new ConditionalLogic(
                expression ?? Expression,
                action ?? Action,
                targetValue ?? TargetValue,
                isEnabled ?? IsEnabled,
                priority ?? Priority,
                description ?? Description,
                new Dictionary<string, object>(Parameters),
                new HashSet<string>(DependentFields),
                evaluationMode ?? EvaluationMode);
        }

        /// <summary>
        /// Adds a dependent field to the conditional logic.
        /// </summary>
        /// <param name="fieldName">The field name to add as dependency.</param>
        /// <returns>A new conditional logic with the added dependent field.</returns>
        public ConditionalLogic WithDependentField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name cannot be null or empty.", nameof(fieldName));

            var newDependentFields = new HashSet<string>(DependentFields) { fieldName };

            return new ConditionalLogic(
                Expression, Action, TargetValue, IsEnabled, Priority, Description,
                new Dictionary<string, object>(Parameters), newDependentFields, EvaluationMode);
        }

        /// <summary>
        /// Adds a parameter to the conditional logic.
        /// </summary>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The parameter value.</param>
        /// <returns>A new conditional logic with the added parameter.</returns>
        public ConditionalLogic WithParameter(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Parameter key cannot be null or empty.", nameof(key));

            var newParameters = new Dictionary<string, object>(Parameters)
            {
                [key] = value
            };

            return new ConditionalLogic(
                Expression, Action, TargetValue, IsEnabled, Priority, Description,
                newParameters, new HashSet<string>(DependentFields), EvaluationMode);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(ConditionalLogic other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Expression == other.Expression &&
                   Action == other.Action &&
                   Equals(TargetValue, other.TargetValue) &&
                   IsEnabled == other.IsEnabled &&
                   Priority == other.Priority &&
                   Description == other.Description &&
                   EvaluationMode == other.EvaluationMode &&
                   Parameters.SequenceEqual(other.Parameters) &&
                   DependentFields.SetEquals(other.DependentFields);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ConditionalLogic);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Expression,
                Action,
                TargetValue,
                IsEnabled,
                Priority,
                Description,
                EvaluationMode);
        }

        /// <summary>
        /// Returns a string representation of the conditional logic.
        /// </summary>
        /// <returns>A string representation of the conditional logic.</returns>
        public override string ToString()
        {
            var parts = new List<string>
            {
                $"Expression: {Expression}",
                $"Action: {Action}"
            };
            
            if (TargetValue != null)
                parts.Add($"Target: {TargetValue}");
            
            if (!IsEnabled)
                parts.Add("Disabled");
            
            if (Priority != 0)
                parts.Add($"Priority: {Priority}");
            
            if (DependentFields.Any())
                parts.Add($"Dependencies: [{string.Join(", ", DependentFields)}]");

            return $"ConditionalLogic({string.Join(", ", parts)})";
        }

        public static bool operator ==(ConditionalLogic left, ConditionalLogic right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ConditionalLogic left, ConditionalLogic right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Converts the conditional logic to JSON string.
        /// </summary>
        /// <returns>JSON representation of the conditional logic.</returns>
        public string ToJson()
        {
            var obj = new
            {
                expression = Expression,
                action = Action.ToString(),
                targetValue = TargetValue,
                isEnabled = IsEnabled,
                priority = Priority,
                description = Description,
                parameters = Parameters,
                dependentFields = DependentFields.ToArray(),
                evaluationMode = EvaluationMode.ToString()
            };
            
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }
    }

    /// <summary>
    /// Represents the action to perform when a conditional logic condition is met.
    /// </summary>
    public enum ConditionalAction
    {
        /// <summary>
        /// Show or hide the field.
        /// </summary>
        SetVisibility,

        /// <summary>
        /// Make the field required or optional.
        /// </summary>
        SetRequired,

        /// <summary>
        /// Enable or disable the field.
        /// </summary>
        SetEnabled,

        /// <summary>
        /// Set the field value.
        /// </summary>
        SetValue,

        /// <summary>
        /// Clear the field value.
        /// </summary>
        ClearValue,

        /// <summary>
        /// Apply validation rules.
        /// </summary>
        SetValidation,

        /// <summary>
        /// Filter available options.
        /// </summary>
        FilterOptions,

        /// <summary>
        /// Set field properties.
        /// </summary>
        SetProperties,

        /// <summary>
        /// Execute a custom action.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Represents the evaluation mode for conditional logic.
    /// </summary>
    public enum ConditionalEvaluationMode
    {
        /// <summary>
        /// Evaluate immediately when dependent field values change.
        /// </summary>
        Immediate,

        /// <summary>
        /// Evaluate when the field loses focus.
        /// </summary>
        OnBlur,

        /// <summary>
        /// Evaluate when the form is submitted.
        /// </summary>
        OnSubmit,

        /// <summary>
        /// Evaluate manually via API call.
        /// </summary>
        Manual
    }
}