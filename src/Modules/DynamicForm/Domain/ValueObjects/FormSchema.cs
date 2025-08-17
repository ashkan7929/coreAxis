using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CoreAxis.Modules.DynamicForm.Domain.ValueObjects
{
    /// <summary>
    /// Represents a complete form schema with fields, validation, and configuration.
    /// </summary>
    public class FormSchema : IEquatable<FormSchema>
    {
        /// <summary>
        /// Gets the schema version.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the form title.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the form description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the form fields.
        /// </summary>
        public IReadOnlyList<FieldDefinition> Fields { get; private set; }

        /// <summary>
        /// Gets the form configuration.
        /// </summary>
        public FormConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets the form-level validation rules.
        /// </summary>
        public IReadOnlyList<ValidationRule> ValidationRules { get; private set; }

        /// <summary>
        /// Gets the form-level conditional logic.
        /// </summary>
        public IReadOnlyList<ConditionalLogic> ConditionalLogic { get; private set; }

        /// <summary>
        /// Gets the form-level formulas.
        /// </summary>
        public IReadOnlyList<FormulaExpression> Formulas { get; private set; }

        /// <summary>
        /// Gets the form sections/groups.
        /// </summary>
        public IReadOnlyList<FormSection> Sections { get; private set; }

        /// <summary>
        /// Gets the form steps for multi-step forms.
        /// </summary>
        public IReadOnlyList<FormStep> Steps { get; private set; }

        /// <summary>
        /// Gets the form layout configuration.
        /// </summary>
        public FormLayout Layout { get; private set; }

        /// <summary>
        /// Gets the form styling configuration.
        /// </summary>
        public FormStyling Styling { get; private set; }

        /// <summary>
        /// Gets the localized content for different languages.
        /// </summary>
        public Dictionary<string, LocalizedContent> Localizations { get; private set; }

        /// <summary>
        /// Gets additional metadata for the form.
        /// </summary>
        public Dictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Gets the schema hash for change detection.
        /// </summary>
        public string Hash { get; private set; }

        /// <summary>
        /// Gets whether the schema is valid.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets the validation errors if the schema is invalid.
        /// </summary>
        public IReadOnlyList<string> ValidationErrors { get; private set; }

        /// <summary>
        /// Gets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets the last modification timestamp.
        /// </summary>
        public DateTime ModifiedAt { get; private set; }

        /// <summary>
        /// Initializes a new instance of the FormSchema class.
        /// </summary>
        /// <param name="version">The schema version.</param>
        /// <param name="title">The form title.</param>
        /// <param name="fields">The form fields.</param>
        /// <param name="configuration">The form configuration.</param>
        public FormSchema(
            string version,
            string title,
            IEnumerable<FieldDefinition> fields,
            FormConfiguration configuration = null)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Schema version cannot be null or empty.", nameof(version));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Form title cannot be null or empty.", nameof(title));
            if (fields == null)
                throw new ArgumentNullException(nameof(fields));

            Version = version.Trim();
            Title = title.Trim();
            Fields = fields.ToList().AsReadOnly();
            Configuration = configuration ?? FormConfiguration.Default();
            
            // Set defaults
            Description = string.Empty;
            ValidationRules = new List<ValidationRule>().AsReadOnly();
            ConditionalLogic = new List<ConditionalLogic>().AsReadOnly();
            Formulas = new List<FormulaExpression>().AsReadOnly();
            Sections = new List<FormSection>().AsReadOnly();
            Steps = new List<FormStep>().AsReadOnly();
            Layout = FormLayout.Default();
            Styling = FormStyling.Default();
            Localizations = new Dictionary<string, LocalizedContent>();
            Metadata = new Dictionary<string, object>();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
            
            Initialize();
        }

        /// <summary>
        /// Creates a simple form schema with basic configuration.
        /// </summary>
        /// <param name="title">The form title.</param>
        /// <param name="fields">The form fields.</param>
        /// <returns>A new form schema.</returns>
        public static FormSchema Create(string title, params FieldDefinition[] fields)
        {
            return new FormSchema("1.0", title, fields);
        }

        /// <summary>
        /// Creates a form schema with custom configuration.
        /// </summary>
        /// <param name="title">The form title.</param>
        /// <param name="configuration">The form configuration.</param>
        /// <param name="fields">The form fields.</param>
        /// <returns>A new form schema.</returns>
        public static FormSchema CreateWithConfiguration(string title, FormConfiguration configuration, params FieldDefinition[] fields)
        {
            return new FormSchema("1.0", title, fields, configuration);
        }

        /// <summary>
        /// Creates a multi-step form schema.
        /// </summary>
        /// <param name="title">The form title.</param>
        /// <param name="steps">The form steps.</param>
        /// <returns>A new multi-step form schema.</returns>
        public static FormSchema CreateMultiStep(string title, params FormStep[] steps)
        {
            var allFields = steps.SelectMany(s => s.Fields).ToArray();
            var schema = new FormSchema("1.0", title, allFields);
            return schema.WithSteps(steps);
        }

        /// <summary>
        /// Creates a sectioned form schema.
        /// </summary>
        /// <param name="title">The form title.</param>
        /// <param name="sections">The form sections.</param>
        /// <returns>A new sectioned form schema.</returns>
        public static FormSchema CreateSectioned(string title, params FormSection[] sections)
        {
            var allFields = sections.SelectMany(s => s.Fields).ToArray();
            var schema = new FormSchema("1.0", title, allFields);
            return schema.WithSections(sections);
        }

        /// <summary>
        /// Creates a survey form schema with specific configuration.
        /// </summary>
        /// <param name="title">The survey title.</param>
        /// <param name="fields">The survey fields.</param>
        /// <returns>A new survey form schema.</returns>
        public static FormSchema CreateSurvey(string title, params FieldDefinition[] fields)
        {
            var config = FormConfiguration.Survey();
            return new FormSchema("1.0", title, fields, config);
        }

        /// <summary>
        /// Creates a registration form schema.
        /// </summary>
        /// <param name="title">The registration form title.</param>
        /// <param name="fields">The registration fields.</param>
        /// <returns>A new registration form schema.</returns>
        public static FormSchema CreateRegistration(string title, params FieldDefinition[] fields)
        {
            var config = FormConfiguration.Secure();
            return new FormSchema("1.0", title, fields, config);
        }

        /// <summary>
        /// Creates a copy of the schema with updated properties.
        /// </summary>
        /// <param name="updates">The properties to update.</param>
        /// <returns>A new form schema with the updated properties.</returns>
        public FormSchema WithUpdates(Action<FormSchemaBuilder> updates)
        {
            if (updates == null)
                throw new ArgumentNullException(nameof(updates));

            var builder = new FormSchemaBuilder(this);
            updates(builder);
            return builder.Build();
        }

        /// <summary>
        /// Creates a copy of the schema with a new description.
        /// </summary>
        /// <param name="description">The new description.</param>
        /// <returns>A new form schema with the updated description.</returns>
        public FormSchema WithDescription(string description)
        {
            return WithUpdates(b => b.Description = description ?? string.Empty);
        }

        /// <summary>
        /// Creates a copy of the schema with additional fields.
        /// </summary>
        /// <param name="fields">The fields to add.</param>
        /// <returns>A new form schema with the added fields.</returns>
        public FormSchema WithFields(params FieldDefinition[] fields)
        {
            return WithUpdates(b => 
            {
                foreach (var field in fields)
                {
                    b.Fields.Add(field);
                }
            });
        }

        /// <summary>
        /// Creates a copy of the schema with a field replaced.
        /// </summary>
        /// <param name="fieldId">The ID of the field to replace.</param>
        /// <param name="newField">The new field definition.</param>
        /// <returns>A new form schema with the replaced field.</returns>
        public FormSchema WithFieldReplaced(string fieldId, FieldDefinition newField)
        {
            return WithUpdates(b => 
            {
                var index = b.Fields.FindIndex(f => f.Id == fieldId);
                if (index >= 0)
                {
                    b.Fields[index] = newField;
                }
            });
        }

        /// <summary>
        /// Creates a copy of the schema without a specific field.
        /// </summary>
        /// <param name="fieldId">The ID of the field to remove.</param>
        /// <returns>A new form schema without the specified field.</returns>
        public FormSchema WithoutField(string fieldId)
        {
            return WithUpdates(b => b.Fields.RemoveAll(f => f.Id == fieldId));
        }

        /// <summary>
        /// Creates a copy of the schema with new configuration.
        /// </summary>
        /// <param name="configuration">The new configuration.</param>
        /// <returns>A new form schema with the updated configuration.</returns>
        public FormSchema WithConfiguration(FormConfiguration configuration)
        {
            return WithUpdates(b => b.Configuration = configuration ?? FormConfiguration.Default());
        }

        /// <summary>
        /// Creates a copy of the schema with additional validation rules.
        /// </summary>
        /// <param name="validationRules">The validation rules to add.</param>
        /// <returns>A new form schema with the added validation rules.</returns>
        public FormSchema WithValidationRules(params ValidationRule[] validationRules)
        {
            return WithUpdates(b => 
            {
                foreach (var rule in validationRules)
                {
                    b.ValidationRules.Add(rule);
                }
            });
        }

        /// <summary>
        /// Creates a copy of the schema with additional conditional logic.
        /// </summary>
        /// <param name="conditionalLogic">The conditional logic to add.</param>
        /// <returns>A new form schema with the added conditional logic.</returns>
        public FormSchema WithConditionalLogic(params ConditionalLogic[] conditionalLogic)
        {
            return WithUpdates(b => 
            {
                foreach (var logic in conditionalLogic)
                {
                    b.ConditionalLogic.Add(logic);
                }
            });
        }

        /// <summary>
        /// Creates a copy of the schema with additional formulas.
        /// </summary>
        /// <param name="formulas">The formulas to add.</param>
        /// <returns>A new form schema with the added formulas.</returns>
        public FormSchema WithFormulas(params FormulaExpression[] formulas)
        {
            return WithUpdates(b => 
            {
                foreach (var formula in formulas)
                {
                    b.Formulas.Add(formula);
                }
            });
        }

        /// <summary>
        /// Creates a copy of the schema with sections.
        /// </summary>
        /// <param name="sections">The sections to set.</param>
        /// <returns>A new form schema with the specified sections.</returns>
        public FormSchema WithSections(params FormSection[] sections)
        {
            return WithUpdates(b => 
            {
                b.Sections.Clear();
                foreach (var section in sections)
                {
                    b.Sections.Add(section);
                }
            });
        }

        /// <summary>
        /// Creates a copy of the schema with steps.
        /// </summary>
        /// <param name="steps">The steps to set.</param>
        /// <returns>A new form schema with the specified steps.</returns>
        public FormSchema WithSteps(params FormStep[] steps)
        {
            return WithUpdates(b => 
            {
                b.Steps.Clear();
                foreach (var step in steps)
                {
                    b.Steps.Add(step);
                }
            });
        }

        /// <summary>
        /// Creates a copy of the schema with new layout.
        /// </summary>
        /// <param name="layout">The new layout.</param>
        /// <returns>A new form schema with the updated layout.</returns>
        public FormSchema WithLayout(FormLayout layout)
        {
            return WithUpdates(b => b.Layout = layout ?? FormLayout.Default());
        }

        /// <summary>
        /// Creates a copy of the schema with new styling.
        /// </summary>
        /// <param name="styling">The new styling.</param>
        /// <returns>A new form schema with the updated styling.</returns>
        public FormSchema WithStyling(FormStyling styling)
        {
            return WithUpdates(b => b.Styling = styling ?? FormStyling.Default());
        }

        /// <summary>
        /// Creates a copy of the schema with localized content.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <param name="content">The localized content.</param>
        /// <returns>A new form schema with the added localization.</returns>
        public FormSchema WithLocalization(string language, LocalizedContent content)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("Language cannot be null or empty.", nameof(language));
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            return WithUpdates(b => b.Localizations[language] = content);
        }

        /// <summary>
        /// Creates a copy of the schema with additional metadata.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        /// <returns>A new form schema with the added metadata.</returns>
        public FormSchema WithMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty.", nameof(key));

            return WithUpdates(b => b.Metadata[key] = value);
        }

        /// <summary>
        /// Gets a field by its ID.
        /// </summary>
        /// <param name="fieldId">The field ID.</param>
        /// <returns>The field definition or null if not found.</returns>
        public FieldDefinition GetField(string fieldId)
        {
            if (string.IsNullOrWhiteSpace(fieldId))
                return null;

            return Fields.FirstOrDefault(f => f.Id == fieldId);
        }

        /// <summary>
        /// Gets a field by its name.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The field definition or null if not found.</returns>
        public FieldDefinition GetFieldByName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return null;

            return Fields.FirstOrDefault(f => f.Name == fieldName);
        }

        /// <summary>
        /// Gets all fields in a specific group.
        /// </summary>
        /// <param name="group">The group name.</param>
        /// <returns>The fields in the specified group.</returns>
        public IEnumerable<FieldDefinition> GetFieldsByGroup(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return Enumerable.Empty<FieldDefinition>();

            return Fields.Where(f => f.Group == group);
        }

        /// <summary>
        /// Gets all required fields.
        /// </summary>
        /// <returns>The required fields.</returns>
        public IEnumerable<FieldDefinition> GetRequiredFields()
        {
            return Fields.Where(f => f.IsRequired);
        }

        /// <summary>
        /// Gets all visible fields.
        /// </summary>
        /// <returns>The visible fields.</returns>
        public IEnumerable<FieldDefinition> GetVisibleFields()
        {
            return Fields.Where(f => f.IsVisible);
        }

        /// <summary>
        /// Gets all calculated fields.
        /// </summary>
        /// <returns>The calculated fields.</returns>
        public IEnumerable<FieldDefinition> GetCalculatedFields()
        {
            return Fields.Where(f => f.Type == FieldType.Calculated);
        }

        /// <summary>
        /// Gets the localized content for the specified language.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The localized content or null if not found.</returns>
        public LocalizedContent GetLocalization(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return null;

            return Localizations.TryGetValue(language, out var content) ? content : null;
        }

        /// <summary>
        /// Gets the localized title for the specified language.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The localized title or the default title if not found.</returns>
        public string GetLocalizedTitle(string language)
        {
            var localization = GetLocalization(language);
            return !string.IsNullOrWhiteSpace(localization?.Title) ? localization.Title : Title;
        }

        /// <summary>
        /// Gets the localized description for the specified language.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <returns>The localized description or the default description if not found.</returns>
        public string GetLocalizedDescription(string language)
        {
            var localization = GetLocalization(language);
            return !string.IsNullOrWhiteSpace(localization?.Description) ? localization.Description : Description;
        }

        /// <summary>
        /// Validates the form schema for consistency and completeness.
        /// </summary>
        /// <returns>True if the schema is valid; otherwise, false.</returns>
        public bool Validate()
        {
            var errors = new List<string>();

            // Validate basic properties
            if (string.IsNullOrWhiteSpace(Version))
                errors.Add("Schema version is required.");
            if (string.IsNullOrWhiteSpace(Title))
                errors.Add("Form title is required.");
            if (!Fields.Any())
                errors.Add("Form must have at least one field.");

            // Validate field uniqueness
            var fieldIds = Fields.Select(f => f.Id).ToList();
            var duplicateIds = fieldIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicateId in duplicateIds)
            {
                errors.Add($"Duplicate field ID: {duplicateId}");
            }

            var fieldNames = Fields.Select(f => f.Name).ToList();
            var duplicateNames = fieldNames.GroupBy(name => name).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicateName in duplicateNames)
            {
                errors.Add($"Duplicate field name: {duplicateName}");
            }

            // Validate individual fields
            foreach (var field in Fields)
            {
                var fieldErrors = field.Validate();
                foreach (var error in fieldErrors)
                {
                    errors.Add($"Field {field.Id}: {error}");
                }
            }

            // Validate conditional logic references
            foreach (var logic in ConditionalLogic)
            {
                foreach (var dependentField in logic.DependentFields)
                {
                    if (!fieldIds.Contains(dependentField))
                    {
                        errors.Add($"Conditional logic references unknown field: {dependentField}");
                    }
                }
            }

            // Validate formula references
            foreach (var formula in Formulas)
            {
                foreach (var variable in formula.Variables)
                {
                    if (!fieldNames.Contains(variable) && !IsSystemVariable(variable))
                    {
                        errors.Add($"Formula references unknown variable: {variable}");
                    }
                }
            }

            // Validate sections
            foreach (var section in Sections)
            {
                foreach (var fieldId in section.FieldIds)
                {
                    if (!fieldIds.Contains(fieldId))
                    {
                        errors.Add($"Section '{section.Id}' references unknown field: {fieldId}");
                    }
                }
            }

            // Validate steps
            foreach (var step in Steps)
            {
                foreach (var field in step.Fields)
                {
                    if (!fieldIds.Contains(field.Id))
                    {
                        errors.Add($"Step '{step.Id}' contains field not in main fields list: {field.Id}");
                    }
                }
            }

            ValidationErrors = errors.AsReadOnly();
            IsValid = !errors.Any();
            return IsValid;
        }

        /// <summary>
        /// Converts the form schema to a JSON representation.
        /// </summary>
        /// <returns>A JSON string representing the form schema.</returns>
        public string ToJson()
        {
            var data = new
            {
                version = Version,
                title = Title,
                description = Description,
                fields = Fields.Select(f => JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(f))).ToArray(),
                configuration = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(Configuration)),
                validationRules = ValidationRules.Select(r => JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(r))).ToArray(),
                conditionalLogic = ConditionalLogic.Select(c => JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(c))).ToArray(),
                formulas = Formulas.Select(f => JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(f))).ToArray(),
                sections = Sections.Select(s => JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(s))).ToArray(),
                steps = Steps.Select(s => JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(s))).ToArray(),
                layout = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(Layout)),
                styling = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(Styling)),
                localizations = Localizations.ToDictionary(kvp => kvp.Key, kvp => JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(kvp.Value))),
                metadata = Metadata,
                hash = Hash,
                isValid = IsValid,
                validationErrors = ValidationErrors.ToArray(),
                createdAt = CreatedAt,
                modifiedAt = ModifiedAt
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        /// <summary>
        /// Creates a form schema from a JSON representation.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>A new form schema.</returns>
        public static FormSchema FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be null or empty.", nameof(json));

            // This is a simplified implementation - in practice, you'd want more robust JSON parsing
            var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var version = root.GetProperty("version").GetString();
            var title = root.GetProperty("title").GetString();
            
            // For now, create a basic schema - full implementation would parse all properties
            var fields = new List<FieldDefinition>();
            
            return new FormSchema(version, title, fields);
        }

        private void Initialize()
        {
            Hash = CalculateHash();
            ValidationErrors = new List<string>().AsReadOnly();
            IsValid = true; // Will be set properly during validation
        }

        private string CalculateHash()
        {
            var content = $"{Version}|{Title}|{string.Join("|", Fields.Select(f => f.Id))}|{Configuration.GetHashCode()}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hash);
        }

        private bool IsSystemVariable(string variable)
        {
            var systemVariables = new[] { "user", "date", "time", "form", "submission", "session" };
            return systemVariables.Contains(variable.ToLower());
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(FormSchema other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Version == other.Version &&
                   Title == other.Title &&
                   Hash == other.Hash;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as FormSchema);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Version, Title, Hash);
        }

        /// <summary>
        /// Returns a string representation of the form schema.
        /// </summary>
        /// <returns>A string representation of the form schema.</returns>
        public override string ToString()
        {
            return $"FormSchema({Version}, {Title}, {Fields.Count} fields)";
        }

        public static bool operator ==(FormSchema left, FormSchema right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FormSchema left, FormSchema right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Represents a form section for grouping related fields.
    /// </summary>
    public class FormSection : IEquatable<FormSection>
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<string> FieldIds { get; }
        public IReadOnlyList<FieldDefinition> Fields { get; }
        public bool IsCollapsible { get; }
        public bool IsCollapsed { get; }
        public int Order { get; }
        public string CssClass { get; }
        public Dictionary<string, object> Metadata { get; }

        public FormSection(string id, string title, IEnumerable<FieldDefinition> fields)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Fields = fields?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(fields));
            FieldIds = Fields.Select(f => f.Id).ToList().AsReadOnly();
            Description = string.Empty;
            IsCollapsible = false;
            IsCollapsed = false;
            Order = 0;
            CssClass = string.Empty;
            Metadata = new Dictionary<string, object>();
        }

        public string ToJson()
        {
            var data = new
            {
                id = Id,
                title = Title,
                description = Description,
                fieldIds = FieldIds.ToArray(),
                isCollapsible = IsCollapsible,
                isCollapsed = IsCollapsed,
                order = Order,
                cssClass = CssClass,
                metadata = Metadata
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        public bool Equals(FormSection other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj) => Equals(obj as FormSection);
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(FormSection left, FormSection right) => Equals(left, right);
        public static bool operator !=(FormSection left, FormSection right) => !Equals(left, right);
    }

    /// <summary>
    /// Represents a form step for multi-step forms.
    /// </summary>
    public class FormStep : IEquatable<FormStep>
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<FieldDefinition> Fields { get; }
        public int Order { get; }
        public bool IsOptional { get; }
        public string NextButtonText { get; }
        public string PreviousButtonText { get; }
        public IReadOnlyList<ValidationRule> ValidationRules { get; }
        public Dictionary<string, object> Metadata { get; }

        public FormStep(string id, string title, IEnumerable<FieldDefinition> fields)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Fields = fields?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(fields));
            Description = string.Empty;
            Order = 0;
            IsOptional = false;
            NextButtonText = "Next";
            PreviousButtonText = "Previous";
            ValidationRules = new List<ValidationRule>().AsReadOnly();
            Metadata = new Dictionary<string, object>();
        }

        public string ToJson()
        {
            var data = new
            {
                id = Id,
                title = Title,
                description = Description,
                fields = Fields.Select(f => JsonSerializer.Deserialize<object>(f.ToJson())).ToArray(),
                order = Order,
                isOptional = IsOptional,
                nextButtonText = NextButtonText,
                previousButtonText = PreviousButtonText,
                validationRules = ValidationRules.Select(r => JsonSerializer.Deserialize<object>(r.ToJson())).ToArray(),
                metadata = Metadata
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        public bool Equals(FormStep other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj) => Equals(obj as FormStep);
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(FormStep left, FormStep right) => Equals(left, right);
        public static bool operator !=(FormStep left, FormStep right) => !Equals(left, right);
    }

    /// <summary>
    /// Represents form layout configuration.
    /// </summary>
    public class FormLayout
    {
        public FormLayoutMode Mode { get; }
        public int Columns { get; }
        public string GridTemplate { get; }
        public Dictionary<string, object> Properties { get; }

        public FormLayout(FormLayoutMode mode = FormLayoutMode.Vertical, int columns = 1, string gridTemplate = null)
        {
            Mode = mode;
            Columns = Math.Max(1, columns);
            GridTemplate = gridTemplate ?? string.Empty;
            Properties = new Dictionary<string, object>();
        }

        public static FormLayout Default() => new FormLayout();
        public static FormLayout Horizontal(int columns = 2) => new FormLayout(FormLayoutMode.Horizontal, columns);
        public static FormLayout Grid(string template) => new FormLayout(FormLayoutMode.Grid, gridTemplate: template);

        public string ToJson()
        {
            var data = new
            {
                mode = Mode.ToString(),
                columns = Columns,
                gridTemplate = GridTemplate,
                properties = Properties
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }

    /// <summary>
    /// Represents form styling configuration.
    /// </summary>
    public class FormStyling
    {
        public string Theme { get; }
        public string CssClass { get; }
        public Dictionary<string, string> CustomCss { get; }
        public Dictionary<string, object> Properties { get; }

        public FormStyling(string theme = "default", string cssClass = null)
        {
            Theme = theme ?? "default";
            CssClass = cssClass ?? string.Empty;
            CustomCss = new Dictionary<string, string>();
            Properties = new Dictionary<string, object>();
        }

        public static FormStyling Default() => new FormStyling();
        public static FormStyling WithTheme(string theme) => new FormStyling(theme);
        public static FormStyling WithCssClass(string cssClass) => new FormStyling(cssClass: cssClass);

        public string ToJson()
        {
            var data = new
            {
                theme = Theme,
                cssClass = CssClass,
                customCss = CustomCss,
                properties = Properties
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }

    /// <summary>
    /// Represents localized content for a form.
    /// </summary>
    public class LocalizedContent
    {
        public string Title { get; }
        public string Description { get; }
        public Dictionary<string, string> FieldLabels { get; }
        public Dictionary<string, string> FieldDescriptions { get; }
        public Dictionary<string, string> FieldPlaceholders { get; }
        public Dictionary<string, string> ValidationMessages { get; }
        public Dictionary<string, string> ButtonTexts { get; }
        public Dictionary<string, object> Metadata { get; }

        public LocalizedContent(string title = null, string description = null)
        {
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            FieldLabels = new Dictionary<string, string>();
            FieldDescriptions = new Dictionary<string, string>();
            FieldPlaceholders = new Dictionary<string, string>();
            ValidationMessages = new Dictionary<string, string>();
            ButtonTexts = new Dictionary<string, string>();
            Metadata = new Dictionary<string, object>();
        }

        public string ToJson()
        {
            var data = new
            {
                title = Title,
                description = Description,
                fieldLabels = FieldLabels,
                fieldDescriptions = FieldDescriptions,
                fieldPlaceholders = FieldPlaceholders,
                validationMessages = ValidationMessages,
                buttonTexts = ButtonTexts,
                metadata = Metadata
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }

    /// <summary>
    /// Builder class for creating form schemas with a fluent interface.
    /// </summary>
    public class FormSchemaBuilder
    {
        public string Version { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<FieldDefinition> Fields { get; set; }
        public FormConfiguration Configuration { get; set; }
        public List<ValidationRule> ValidationRules { get; set; }
        public List<ConditionalLogic> ConditionalLogic { get; set; }
        public List<FormulaExpression> Formulas { get; set; }
        public List<FormSection> Sections { get; set; }
        public List<FormStep> Steps { get; set; }
        public FormLayout Layout { get; set; }
        public FormStyling Styling { get; set; }
        public Dictionary<string, LocalizedContent> Localizations { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public FormSchemaBuilder(FormSchema schema)
        {
            Version = schema.Version;
            Title = schema.Title;
            Description = schema.Description;
            Fields = new List<FieldDefinition>(schema.Fields);
            Configuration = schema.Configuration;
            ValidationRules = new List<ValidationRule>(schema.ValidationRules);
            ConditionalLogic = new List<ConditionalLogic>(schema.ConditionalLogic);
            Formulas = new List<FormulaExpression>(schema.Formulas);
            Sections = new List<FormSection>(schema.Sections);
            Steps = new List<FormStep>(schema.Steps);
            Layout = schema.Layout;
            Styling = schema.Styling;
            Localizations = new Dictionary<string, LocalizedContent>(schema.Localizations);
            Metadata = new Dictionary<string, object>(schema.Metadata);
        }

        public FormSchema Build()
        {
            var schema = new FormSchema(Version, Title, Fields, Configuration);
            
            // Use reflection to set private properties
            var schemaType = typeof(FormSchema);
            
            schemaType.GetProperty(nameof(Description))?.SetValue(schema, Description);
            schemaType.GetProperty(nameof(ValidationRules))?.SetValue(schema, ValidationRules.AsReadOnly());
            schemaType.GetProperty(nameof(ConditionalLogic))?.SetValue(schema, ConditionalLogic.AsReadOnly());
            schemaType.GetProperty(nameof(Formulas))?.SetValue(schema, Formulas.AsReadOnly());
            schemaType.GetProperty(nameof(Sections))?.SetValue(schema, Sections.AsReadOnly());
            schemaType.GetProperty(nameof(Steps))?.SetValue(schema, Steps.AsReadOnly());
            schemaType.GetProperty(nameof(Layout))?.SetValue(schema, Layout);
            schemaType.GetProperty(nameof(Styling))?.SetValue(schema, Styling);
            schemaType.GetProperty(nameof(Localizations))?.SetValue(schema, Localizations);
            schemaType.GetProperty(nameof(Metadata))?.SetValue(schema, Metadata);
            schemaType.GetProperty(nameof(FormSchema.ModifiedAt))?.SetValue(schema, DateTime.UtcNow);
            
            return schema;
        }
    }
}