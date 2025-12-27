using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Domain.ValueObjects
{
    /// <summary>
    /// Represents a complete field definition with all its properties, validation rules, and behavior.
    /// </summary>
    public class FieldDefinition : IEquatable<FieldDefinition>
    {
        /// <summary>
        /// Gets the unique identifier for the field.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the field name (used for data binding).
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the field type.
        /// </summary>
        public FieldType Type { get; private set; }

        /// <summary>
        /// Gets the display label for the field.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Gets the description or help text for the field.
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        /// Gets the placeholder text for input fields.
        /// </summary>
        public string Placeholder { get; init; }

        /// <summary>
        /// Gets the default value for the field.
        /// </summary>
        public object DefaultValue { get; init; }

        /// <summary>
        /// Gets whether the field is required.
        /// </summary>
        public bool IsRequired { get; init; }

        /// <summary>
        /// Gets whether the field is enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets whether the field is visible.
        /// </summary>
        public bool IsVisible { get; init; }

        /// <summary>
        /// Gets whether the field is read-only.
        /// </summary>
        public bool IsReadOnly { get; init; }

        /// <summary>
        /// Gets the display order of the field.
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        /// Gets the group or section the field belongs to.
        /// </summary>
        public string Group { get; init; }

        /// <summary>
        /// Gets the CSS classes to apply to the field.
        /// </summary>
        public string CssClass { get; init; }

        /// <summary>
        /// Gets the validation rules for the field.
        /// </summary>
        public IReadOnlyList<ValidationRule> ValidationRules { get; init; }

        /// <summary>
        /// Gets the options for select, radio, and checkbox fields.
        /// </summary>
        public IReadOnlyList<FieldOption> Options { get; init; }

        /// <summary>
        /// Gets the conditional logic rules for the field.
        /// </summary>
        public IReadOnlyList<ConditionalLogic> ConditionalLogic { get; init; }

        /// <summary>
        /// Gets the formula expressions associated with the field.
        /// </summary>
        public IReadOnlyList<FormulaExpression> Formulas { get; init; }

        /// <summary>
        /// Gets the field-specific configuration properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; init; }

        /// <summary>
        /// Gets the localized labels for different languages.
        /// </summary>
        public Dictionary<string, string> LocalizedLabels { get; init; }

        /// <summary>
        /// Gets the localized descriptions for different languages.
        /// </summary>
        public Dictionary<string, string> LocalizedDescriptions { get; init; }

        /// <summary>
        /// Gets the localized placeholders for different languages.
        /// </summary>
        public Dictionary<string, string> LocalizedPlaceholders { get; init; }

        /// <summary>
        /// Gets additional metadata for the field.
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; }

        /// <summary>
        /// Gets the field definition version.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets the last modification timestamp.
        /// </summary>
        public DateTime ModifiedAt { get; private set; }

        /// <summary>
        /// Initializes a new instance of the FieldDefinition class.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="type">The field type.</param>
        /// <param name="label">The field label.</param>
        public FieldDefinition(
            string id,
            string name,
            FieldType type,
            string label)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Field ID cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Field name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Field label cannot be null or empty.", nameof(label));

            Id = id.Trim();
            Name = name.Trim();
            Type = type;
            Label = label.Trim();
            
            // Set defaults
            Description = string.Empty;
            Placeholder = string.Empty;
            DefaultValue = null;
            IsRequired = false;
            IsEnabled = true;
            IsVisible = true;
            IsReadOnly = false;
            Order = 0;
            Group = string.Empty;
            CssClass = string.Empty;
            ValidationRules = new List<ValidationRule>().AsReadOnly();
            Options = new List<FieldOption>().AsReadOnly();
            ConditionalLogic = new List<ConditionalLogic>().AsReadOnly();
            Formulas = new List<FormulaExpression>().AsReadOnly();
            Properties = new Dictionary<string, object>();
            LocalizedLabels = new Dictionary<string, string>();
            LocalizedDescriptions = new Dictionary<string, string>();
            LocalizedPlaceholders = new Dictionary<string, string>();
            Metadata = new Dictionary<string, object>();
            Version = "1.0";
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a text input field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="placeholder">The placeholder text.</param>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <returns>A new text field definition.</returns>
        public static FieldDefinition CreateText(string id, string name, string label, string placeholder = null, bool isRequired = false)
        {
            var field = new FieldDefinition(id, name, FieldType.Text, label);
            return field.WithPlaceholder(placeholder ?? string.Empty)
                       .WithRequired(isRequired);
        }

        /// <summary>
        /// Creates a number input field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <param name="step">The step value.</param>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <returns>A new number field definition.</returns>
        public static FieldDefinition CreateNumber(string id, string name, string label, decimal? min = null, decimal? max = null, decimal? step = null, bool isRequired = false)
        {
            var field = new FieldDefinition(id, name, FieldType.Number, label)
                .WithRequired(isRequired);

            if (min.HasValue)
                field = field.WithProperty("min", min.Value);
            if (max.HasValue)
                field = field.WithProperty("max", max.Value);
            if (step.HasValue)
                field = field.WithProperty("step", step.Value);

            return field;
        }

        /// <summary>
        /// Creates an email input field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="placeholder">The placeholder text.</param>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <returns>A new email field definition.</returns>
        public static FieldDefinition CreateEmail(string id, string name, string label, string placeholder = null, bool isRequired = false)
        {
            var field = new FieldDefinition(id, name, FieldType.Email, label)
                .WithPlaceholder(placeholder ?? "Enter your email address")
                .WithRequired(isRequired)
                .WithValidationRule(ValidationRule.Email("Please enter a valid email address."));

            return field;
        }

        /// <summary>
        /// Creates a password input field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="minLength">The minimum password length.</param>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <returns>A new password field definition.</returns>
        public static FieldDefinition CreatePassword(string id, string name, string label, int minLength = 8, bool isRequired = false)
        {
            var field = new FieldDefinition(id, name, FieldType.Password, label)
                .WithRequired(isRequired)
                .WithValidationRule(ValidationRule.MinLength(minLength, $"Password must be at least {minLength} characters long."));

            return field;
        }

        /// <summary>
        /// Creates a textarea field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="rows">The number of rows.</param>
        /// <param name="maxLength">The maximum character length.</param>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <returns>A new textarea field definition.</returns>
        public static FieldDefinition CreateTextarea(string id, string name, string label, int rows = 4, int? maxLength = null, bool isRequired = false)
        {
            var field = new FieldDefinition(id, name, FieldType.Textarea, label)
                .WithRequired(isRequired)
                .WithProperty("rows", rows);

            if (maxLength.HasValue)
                field = field.WithValidationRule(ValidationRule.MaxLength(maxLength.Value, $"Text cannot exceed {maxLength} characters."));

            return field;
        }

        /// <summary>
        /// Creates a select dropdown field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="options">The select options.</param>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <param name="allowMultiple">Whether multiple selection is allowed.</param>
        /// <returns>A new select field definition.</returns>
        public static FieldDefinition CreateSelect(string id, string name, string label, IEnumerable<FieldOption> options, bool isRequired = false, bool allowMultiple = false)
        {
            var fieldType = allowMultiple ? FieldType.MultiSelect : FieldType.Select;
            var field = new FieldDefinition(id, name, fieldType, label)
                .WithRequired(isRequired)
                .WithOptions(options);

            return field;
        }

        /// <summary>
        /// Creates a radio button group field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="options">The radio options.</param>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <returns>A new radio field definition.</returns>
        public static FieldDefinition CreateRadio(string id, string name, string label, IEnumerable<FieldOption> options, bool isRequired = false)
        {
            var field = new FieldDefinition(id, name, FieldType.Radio, label)
                .WithRequired(isRequired)
                .WithOptions(options);

            return field;
        }

        /// <summary>
        /// Creates a checkbox group field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="options">The checkbox options.</param>
        /// <param name="isRequired">Whether at least one option is required.</param>
        /// <returns>A new checkbox field definition.</returns>
        public static FieldDefinition CreateCheckbox(string id, string name, string label, IEnumerable<FieldOption> options, bool isRequired = false)
        {
            var field = new FieldDefinition(id, name, FieldType.Checkbox, label)
                .WithRequired(isRequired)
                .WithOptions(options);

            return field;
        }

        /// <summary>
        /// Creates a single checkbox field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="isRequired">Whether the checkbox must be checked.</param>
        /// <returns>A new boolean field definition.</returns>
        public static FieldDefinition CreateBoolean(string id, string name, string label, bool isRequired = false)
        {
            var field = new FieldDefinition(id, name, FieldType.Boolean, label)
                .WithRequired(isRequired)
                .WithDefaultValue(false);

            return field;
        }

        /// <summary>
        /// Creates a date input field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="minDate">The minimum allowed date.</param>
        /// <param name="maxDate">The maximum allowed date.</param>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <returns>A new date field definition.</returns>
        public static FieldDefinition CreateDate(string id, string name, string label, DateTime? minDate = null, DateTime? maxDate = null, bool isRequired = false)
        {
            var field = new FieldDefinition(id, name, FieldType.Date, label)
                .WithRequired(isRequired);

            if (minDate.HasValue)
                field = field.WithProperty("min", minDate.Value.ToString("yyyy-MM-dd"));
            if (maxDate.HasValue)
                field = field.WithProperty("max", maxDate.Value.ToString("yyyy-MM-dd"));

            return field;
        }

        /// <summary>
        /// Creates a file upload field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="acceptedTypes">The accepted file types (MIME types).</param>
        /// <param name="maxSize">The maximum file size in bytes.</param>
        /// <param name="allowMultiple">Whether multiple files are allowed.</param>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <returns>A new file field definition.</returns>
        public static FieldDefinition CreateFile(string id, string name, string label, string[] acceptedTypes = null, long? maxSize = null, bool allowMultiple = false, bool isRequired = false)
        {
            var field = new FieldDefinition(id, name, FieldType.File, label)
                .WithRequired(isRequired)
                .WithProperty("multiple", allowMultiple);

            if (acceptedTypes != null && acceptedTypes.Length > 0)
                field = field.WithProperty("accept", string.Join(",", acceptedTypes));
            if (maxSize.HasValue)
                field = field.WithProperty("maxSize", maxSize.Value);

            return field;
        }

        /// <summary>
        /// Creates a hidden field.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="value">The hidden value.</param>
        /// <returns>A new hidden field definition.</returns>
        public static FieldDefinition CreateHidden(string id, string name, object value)
        {
            var field = new FieldDefinition(id, name, FieldType.Hidden, string.Empty)
                .WithDefaultValue(value)
                .WithVisible(false);

            return field;
        }

        /// <summary>
        /// Creates a calculated field with a formula.
        /// </summary>
        /// <param name="id">The field identifier.</param>
        /// <param name="name">The field name.</param>
        /// <param name="label">The field label.</param>
        /// <param name="formula">The calculation formula.</param>
        /// <returns>A new calculated field definition.</returns>
        public static FieldDefinition CreateCalculated(string id, string name, string label, FormulaExpression formula)
        {
            var field = new FieldDefinition(id, name, FieldType.Calculated, label)
                .WithReadOnly(true)
                .WithFormula(formula);

            return field;
        }

        /// <summary>
        /// Creates a copy of the field definition with updated properties.
        /// </summary>
        /// <param name="updates">The properties to update.</param>
        /// <returns>A new field definition with the updated properties.</returns>
        public FieldDefinition WithUpdates(Action<FieldDefinitionBuilder> updates)
        {
            if (updates == null)
                throw new ArgumentNullException(nameof(updates));

            var builder = new FieldDefinitionBuilder(this);
            updates(builder);
            return builder.Build();
        }

        /// <summary>
        /// Creates a copy of the field definition with a new description.
        /// </summary>
        /// <param name="description">The new description.</param>
        /// <returns>A new field definition with the updated description.</returns>
        public FieldDefinition WithDescription(string description)
        {
            return WithUpdates(b => b.Description = description ?? string.Empty);
        }

        /// <summary>
        /// Creates a copy of the field definition with a new placeholder.
        /// </summary>
        /// <param name="placeholder">The new placeholder.</param>
        /// <returns>A new field definition with the updated placeholder.</returns>
        public FieldDefinition WithPlaceholder(string placeholder)
        {
            return WithUpdates(b => b.Placeholder = placeholder ?? string.Empty);
        }

        /// <summary>
        /// Creates a copy of the field definition with a new default value.
        /// </summary>
        /// <param name="defaultValue">The new default value.</param>
        /// <returns>A new field definition with the updated default value.</returns>
        public FieldDefinition WithDefaultValue(object defaultValue)
        {
            return WithUpdates(b => b.DefaultValue = defaultValue);
        }

        /// <summary>
        /// Creates a copy of the field definition with updated required status.
        /// </summary>
        /// <param name="isRequired">Whether the field is required.</param>
        /// <returns>A new field definition with the updated required status.</returns>
        public FieldDefinition WithRequired(bool isRequired)
        {
            return WithUpdates(b => b.IsRequired = isRequired);
        }

        /// <summary>
        /// Creates a copy of the field definition with updated enabled status.
        /// </summary>
        /// <param name="isEnabled">Whether the field is enabled.</param>
        /// <returns>A new field definition with the updated enabled status.</returns>
        public FieldDefinition WithEnabled(bool isEnabled)
        {
            return WithUpdates(b => b.IsEnabled = isEnabled);
        }

        /// <summary>
        /// Creates a copy of the field definition with updated visibility.
        /// </summary>
        /// <param name="isVisible">Whether the field is visible.</param>
        /// <returns>A new field definition with the updated visibility.</returns>
        public FieldDefinition WithVisible(bool isVisible)
        {
            return WithUpdates(b => b.IsVisible = isVisible);
        }

        /// <summary>
        /// Creates a copy of the field definition with updated read-only status.
        /// </summary>
        /// <param name="isReadOnly">Whether the field is read-only.</param>
        /// <returns>A new field definition with the updated read-only status.</returns>
        public FieldDefinition WithReadOnly(bool isReadOnly)
        {
            return WithUpdates(b => b.IsReadOnly = isReadOnly);
        }

        /// <summary>
        /// Creates a copy of the field definition with a new display order.
        /// </summary>
        /// <param name="order">The new display order.</param>
        /// <returns>A new field definition with the updated order.</returns>
        public FieldDefinition WithOrder(int order)
        {
            return WithUpdates(b => b.Order = order);
        }

        /// <summary>
        /// Creates a copy of the field definition with a new group.
        /// </summary>
        /// <param name="group">The new group.</param>
        /// <returns>A new field definition with the updated group.</returns>
        public FieldDefinition WithGroup(string group)
        {
            return WithUpdates(b => b.Group = group ?? string.Empty);
        }

        /// <summary>
        /// Creates a copy of the field definition with new CSS classes.
        /// </summary>
        /// <param name="cssClass">The new CSS classes.</param>
        /// <returns>A new field definition with the updated CSS classes.</returns>
        public FieldDefinition WithCssClass(string cssClass)
        {
            return WithUpdates(b => b.CssClass = cssClass ?? string.Empty);
        }

        /// <summary>
        /// Creates a copy of the field definition with an additional validation rule.
        /// </summary>
        /// <param name="validationRule">The validation rule to add.</param>
        /// <returns>A new field definition with the added validation rule.</returns>
        public FieldDefinition WithValidationRule(ValidationRule validationRule)
        {
            if (validationRule == null)
                throw new ArgumentNullException(nameof(validationRule));

            return WithUpdates(b => b.ValidationRules.Add(validationRule));
        }

        /// <summary>
        /// Creates a copy of the field definition with new validation rules.
        /// </summary>
        /// <param name="validationRules">The validation rules to set.</param>
        /// <returns>A new field definition with the updated validation rules.</returns>
        public FieldDefinition WithValidationRules(IEnumerable<ValidationRule> validationRules)
        {
            return WithUpdates(b => 
            {
                b.ValidationRules.Clear();
                if (validationRules != null)
                {
                    foreach (var rule in validationRules)
                    {
                        b.ValidationRules.Add(rule);
                    }
                }
            });
        }

        /// <summary>
        /// Creates a copy of the field definition with new options.
        /// </summary>
        /// <param name="options">The options to set.</param>
        /// <returns>A new field definition with the updated options.</returns>
        public FieldDefinition WithOptions(IEnumerable<FieldOption> options)
        {
            return WithUpdates(b => 
            {
                b.Options.Clear();
                if (options != null)
                {
                    foreach (var option in options)
                    {
                        b.Options.Add(option);
                    }
                }
            });
        }

        /// <summary>
        /// Creates a copy of the field definition with an additional conditional logic rule.
        /// </summary>
        /// <param name="conditionalLogic">The conditional logic rule to add.</param>
        /// <returns>A new field definition with the added conditional logic rule.</returns>
        public FieldDefinition WithConditionalLogic(ConditionalLogic conditionalLogic)
        {
            if (conditionalLogic == null)
                throw new ArgumentNullException(nameof(conditionalLogic));

            return WithUpdates(b => b.ConditionalLogic.Add(conditionalLogic));
        }

        /// <summary>
        /// Creates a copy of the field definition with an additional formula.
        /// </summary>
        /// <param name="formula">The formula to add.</param>
        /// <returns>A new field definition with the added formula.</returns>
        public FieldDefinition WithFormula(FormulaExpression formula)
        {
            if (formula == null)
                throw new ArgumentNullException(nameof(formula));

            return WithUpdates(b => b.Formulas.Add(formula));
        }

        /// <summary>
        /// Creates a copy of the field definition with an additional property.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="value">The property value.</param>
        /// <returns>A new field definition with the added property.</returns>
        public FieldDefinition WithProperty(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key cannot be null or empty.", nameof(key));

            return WithUpdates(b => b.Properties[key] = value);
        }

        /// <summary>
        /// Creates a copy of the field definition with a localized label.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <param name="label">The localized label.</param>
        /// <returns>A new field definition with the added localized label.</returns>
        public FieldDefinition WithLocalizedLabel(string language, string label)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("Language cannot be null or empty.", nameof(language));

            return WithUpdates(b => b.LocalizedLabels[language] = label ?? string.Empty);
        }

        /// <summary>
        /// Creates a copy of the field definition with additional metadata.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        /// <returns>A new field definition with the added metadata.</returns>
        public FieldDefinition WithMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty.", nameof(key));

            return WithUpdates(b => b.Metadata[key] = value);
        }

        /// <summary>
        /// Gets the localized label for the specified language.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The localized label or the default label if not found.</returns>
        public string GetLocalizedLabel(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return Label;

            return LocalizedLabels.TryGetValue(language, out var localizedLabel) && !string.IsNullOrWhiteSpace(localizedLabel)
                ? localizedLabel
                : Label;
        }

        /// <summary>
        /// Gets the localized description for the specified language.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The localized description or the default description if not found.</returns>
        public string GetLocalizedDescription(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return Description;

            return LocalizedDescriptions.TryGetValue(language, out var localizedDescription) && !string.IsNullOrWhiteSpace(localizedDescription)
                ? localizedDescription
                : Description;
        }

        /// <summary>
        /// Gets the localized placeholder for the specified language.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The localized placeholder or the default placeholder if not found.</returns>
        public string GetLocalizedPlaceholder(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return Placeholder;

            return LocalizedPlaceholders.TryGetValue(language, out var localizedPlaceholder) && !string.IsNullOrWhiteSpace(localizedPlaceholder)
                ? localizedPlaceholder
                : Placeholder;
        }

        /// <summary>
        /// Validates the field definition for consistency and completeness.
        /// </summary>
        /// <returns>A collection of validation errors, if any.</returns>
        public IEnumerable<string> Validate()
        {
            var errors = new List<string>();

            // Validate required properties
            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Field ID is required.");
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Field name is required.");
            if (string.IsNullOrWhiteSpace(Label))
                errors.Add("Field label is required.");

            // Validate field type specific requirements
            if (Type == FieldType.Select || Type == FieldType.MultiSelect || Type == FieldType.Radio || Type == FieldType.Checkbox)
            {
                if (!Options.Any())
                    errors.Add($"Field type {Type} requires at least one option.");
            }

            // Validate validation rules
            foreach (var rule in ValidationRules)
            {
                if (rule == null)
                    errors.Add("Validation rule cannot be null.");
            }

            // Validate conditional logic
            foreach (var logic in ConditionalLogic)
            {
                if (logic == null)
                    errors.Add("Conditional logic cannot be null.");
            }

            // Validate formulas
            foreach (var formula in Formulas)
            {
                if (formula == null)
                    errors.Add("Formula cannot be null.");
                else if (!formula.IsValid)
                    errors.Add($"Formula is invalid: {string.Join(", ", formula.ValidationErrors)}");
            }

            return errors;
        }

        /// <summary>
        /// Converts the field definition to a JSON representation.
        /// </summary>
        /// <returns>A JSON string representing the field definition.</returns>
        public string ToJson()
        {
            var data = new
            {
                id = Id,
                name = Name,
                type = Type.ToString(),
                label = Label,
                description = Description,
                placeholder = Placeholder,
                defaultValue = DefaultValue,
                isRequired = IsRequired,
                isEnabled = IsEnabled,
                isVisible = IsVisible,
                isReadOnly = IsReadOnly,
                order = Order,
                group = Group,
                cssClass = CssClass,
                validationRules = ValidationRules.Select(r => r.ToJson()).ToArray(),
                options = Options.Select(o => o.ToJson()).ToArray(),
                conditionalLogic = ConditionalLogic.Select(c => c.ToJson()).ToArray(),
                formulas = Formulas.Select(f => f.ToJson()).ToArray(),
                properties = Properties,
                localizedLabels = LocalizedLabels,
                localizedDescriptions = LocalizedDescriptions,
                localizedPlaceholders = LocalizedPlaceholders,
                metadata = Metadata,
                version = Version,
                createdAt = CreatedAt,
                modifiedAt = ModifiedAt
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(FieldDefinition other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Id == other.Id &&
                   Name == other.Name &&
                   Type == other.Type &&
                   Label == other.Label;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as FieldDefinition);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Type, Label);
        }

        /// <summary>
        /// Returns a string representation of the field definition.
        /// </summary>
        /// <returns>A string representation of the field definition.</returns>
        public override string ToString()
        {
            return $"FieldDefinition({Id}, {Name}, {Type}, {Label})";
        }

        public static bool operator ==(FieldDefinition left, FieldDefinition right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FieldDefinition left, FieldDefinition right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Represents the type of a form field.
    /// </summary>
    public enum FieldType
    {
        /// <summary>
        /// Text input field.
        /// </summary>
        Text,

        /// <summary>
        /// Number input field.
        /// </summary>
        Number,

        /// <summary>
        /// Email input field.
        /// </summary>
        Email,

        /// <summary>
        /// Password input field.
        /// </summary>
        Password,

        /// <summary>
        /// Textarea field.
        /// </summary>
        Textarea,

        /// <summary>
        /// Select dropdown field.
        /// </summary>
        Select,

        /// <summary>
        /// Multi-select dropdown field.
        /// </summary>
        MultiSelect,

        /// <summary>
        /// Radio button group field.
        /// </summary>
        Radio,

        /// <summary>
        /// Checkbox group field.
        /// </summary>
        Checkbox,

        /// <summary>
        /// Single checkbox (boolean) field.
        /// </summary>
        Boolean,

        /// <summary>
        /// Date input field.
        /// </summary>
        Date,

        /// <summary>
        /// Time input field.
        /// </summary>
        Time,

        /// <summary>
        /// DateTime input field.
        /// </summary>
        DateTime,

        /// <summary>
        /// File upload field.
        /// </summary>
        File,

        /// <summary>
        /// Hidden field.
        /// </summary>
        Hidden,

        /// <summary>
        /// Calculated field (read-only, computed from formula).
        /// </summary>
        Calculated,

        /// <summary>
        /// Display-only field (no input, just shows content).
        /// </summary>
        Display,

        /// <summary>
        /// Section header or divider.
        /// </summary>
        Section,

        /// <summary>
        /// URL input field.
        /// </summary>
        Url,

        /// <summary>
        /// Phone number input field.
        /// </summary>
        Phone,

        /// <summary>
        /// Color picker field.
        /// </summary>
        Color,

        /// <summary>
        /// Range slider field.
        /// </summary>
        Range
    }

    /// <summary>
    /// Builder class for creating field definitions with a fluent interface.
    /// </summary>
    public class FieldDefinitionBuilder
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public FieldType Type { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string Placeholder { get; set; }
        public object DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsVisible { get; set; }
        public bool IsReadOnly { get; set; }
        public int Order { get; set; }
        public string Group { get; set; }
        public string CssClass { get; set; }
        public List<ValidationRule> ValidationRules { get; set; }
        public List<FieldOption> Options { get; set; }
        public List<ConditionalLogic> ConditionalLogic { get; set; }
        public List<FormulaExpression> Formulas { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public Dictionary<string, string> LocalizedLabels { get; set; }
        public Dictionary<string, string> LocalizedDescriptions { get; set; }
        public Dictionary<string, string> LocalizedPlaceholders { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public string Version { get; set; }

        public FieldDefinitionBuilder(FieldDefinition field)
        {
            Id = field.Id;
            Name = field.Name;
            Type = field.Type;
            Label = field.Label;
            Description = field.Description;
            Placeholder = field.Placeholder;
            DefaultValue = field.DefaultValue;
            IsRequired = field.IsRequired;
            IsEnabled = field.IsEnabled;
            IsVisible = field.IsVisible;
            IsReadOnly = field.IsReadOnly;
            Order = field.Order;
            Group = field.Group;
            CssClass = field.CssClass;
            ValidationRules = new List<ValidationRule>(field.ValidationRules);
            Options = new List<FieldOption>(field.Options);
            ConditionalLogic = new List<ConditionalLogic>(field.ConditionalLogic);
            Formulas = new List<FormulaExpression>(field.Formulas);
            Properties = new Dictionary<string, object>(field.Properties);
            LocalizedLabels = new Dictionary<string, string>(field.LocalizedLabels);
            LocalizedDescriptions = new Dictionary<string, string>(field.LocalizedDescriptions);
            LocalizedPlaceholders = new Dictionary<string, string>(field.LocalizedPlaceholders);
            Metadata = new Dictionary<string, object>(field.Metadata);
            Version = field.Version;
        }

        public FieldDefinition Build()
        {
            var field = new FieldDefinition(Id, Name, Type, Label);
            
            // Use reflection to set private fields
            var fieldType = typeof(FieldDefinition);
            
            fieldType.GetProperty(nameof(Description))?.SetValue(field, Description);
            fieldType.GetProperty(nameof(Placeholder))?.SetValue(field, Placeholder);
            fieldType.GetProperty(nameof(DefaultValue))?.SetValue(field, DefaultValue);
            fieldType.GetProperty(nameof(IsRequired))?.SetValue(field, IsRequired);
            fieldType.GetProperty(nameof(IsEnabled))?.SetValue(field, IsEnabled);
            fieldType.GetProperty(nameof(IsVisible))?.SetValue(field, IsVisible);
            fieldType.GetProperty(nameof(IsReadOnly))?.SetValue(field, IsReadOnly);
            fieldType.GetProperty(nameof(Order))?.SetValue(field, Order);
            fieldType.GetProperty(nameof(Group))?.SetValue(field, Group);
            fieldType.GetProperty(nameof(CssClass))?.SetValue(field, CssClass);
            fieldType.GetProperty(nameof(ValidationRules))?.SetValue(field, ValidationRules.AsReadOnly());
            fieldType.GetProperty(nameof(Options))?.SetValue(field, Options.AsReadOnly());
            fieldType.GetProperty(nameof(ConditionalLogic))?.SetValue(field, ConditionalLogic.AsReadOnly());
            fieldType.GetProperty(nameof(Formulas))?.SetValue(field, Formulas.AsReadOnly());
            fieldType.GetProperty(nameof(Properties))?.SetValue(field, Properties);
            fieldType.GetProperty(nameof(LocalizedLabels))?.SetValue(field, LocalizedLabels);
            fieldType.GetProperty(nameof(LocalizedDescriptions))?.SetValue(field, LocalizedDescriptions);
            fieldType.GetProperty(nameof(LocalizedPlaceholders))?.SetValue(field, LocalizedPlaceholders);
            fieldType.GetProperty(nameof(Metadata))?.SetValue(field, Metadata);
            fieldType.GetProperty(nameof(Version))?.SetValue(field, Version);
            fieldType.GetProperty(nameof(FieldDefinition.ModifiedAt))?.SetValue(field, DateTime.UtcNow);
            
            return field;
        }
    }
}