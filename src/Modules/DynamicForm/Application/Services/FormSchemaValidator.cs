using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;

using CoreAxis.SharedKernel;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Service for validating form schemas.
    /// </summary>
    public class FormSchemaValidator : IFormSchemaValidator
    {
        private readonly ILogger<FormSchemaValidator> _logger;
        private static readonly string[] SupportedVersions = { "1.0", "1.1", "1.2" };
        private static readonly string[] RequiredFieldTypes = 
        {
            "text", "number", "email", "password", "textarea", "select", "multiselect",
            "checkbox", "radio", "date", "datetime", "time", "file", "hidden"
        };

        public FormSchemaValidator(ILogger<FormSchemaValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> ValidateAsync(FormSchema schema)
        {
            try
            {
                if (schema == null)
                {
                    return Result<bool>.Failure("Schema cannot be null");
                }

                var errors = new List<string>();

                // Validate structure
                var structureErrors = await ValidateStructureAsync(schema);
                errors.AddRange(structureErrors);

                // Validate fields
                var fieldErrors = await ValidateFieldsAsync(schema.Fields);
                errors.AddRange(fieldErrors);

                // Validate conditional logic
                var conditionalErrors = await ValidateConditionalLogicAsync(schema);
                errors.AddRange(conditionalErrors);

                // Validate formulas
                var formulaErrors = await ValidateFormulasAsync(schema);
                errors.AddRange(formulaErrors);

                if (errors.Any())
                {
                    var errorMessage = string.Join("; ", errors);
                    _logger.LogWarning("Schema validation failed: {Errors}", errorMessage);
                    return Result<bool>.Failure(errorMessage);
                }

                _logger.LogInformation("Schema validation successful");
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during schema validation");
                return Result<bool>.Failure($"Validation error: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ValidateJsonAsync(string schemaJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(schemaJson))
                {
                    return Result<bool>.Failure("Schema JSON cannot be null or empty");
                }

                // Try to parse JSON
                FormSchema schema;
                try
                {
                    schema = JsonSerializer.Deserialize<FormSchema>(schemaJson);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("Invalid JSON format: {Error}", ex.Message);
                    return Result<bool>.Failure($"Invalid JSON format: {ex.Message}");
                }

                return await ValidateAsync(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during JSON schema validation");
                return Result<bool>.Failure($"JSON validation error: {ex.Message}");
            }
        }

        public async Task<IEnumerable<string>> ValidateFieldsAsync(IEnumerable<FieldDefinition> fields)
        {
            var errors = new List<string>();

            if (fields == null || !fields.Any())
            {
                errors.Add("Schema must contain at least one field");
                return errors;
            }

            var fieldIds = new HashSet<string>();
            var fieldNames = new HashSet<string>();

            foreach (var field in fields)
            {
                // Check for duplicate IDs
                if (!fieldIds.Add(field.Id))
                {
                    errors.Add($"Duplicate field ID: {field.Id}");
                }

                // Check for duplicate names
                if (!fieldNames.Add(field.Name))
                {
                    errors.Add($"Duplicate field name: {field.Name}");
                }

                // Validate field type
                if (!RequiredFieldTypes.Contains(field.Type.ToString().ToLowerInvariant()))
                {
                    errors.Add($"Unsupported field type '{field.Type}' for field '{field.Name}'");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(field.Name))
                {
                    errors.Add($"Field name is required for field ID: {field.Id}");
                }

                if (string.IsNullOrWhiteSpace(field.Label))
                {
                    errors.Add($"Field label is required for field: {field.Name}");
                }

                // Validate select/multiselect options
                if ((field.Type.ToString().Equals("select", StringComparison.OrdinalIgnoreCase) ||
                     field.Type.ToString().Equals("multiselect", StringComparison.OrdinalIgnoreCase) ||
                     field.Type.ToString().Equals("radio", StringComparison.OrdinalIgnoreCase)) &&
                    (field.Options == null || !field.Options.Any()))
                {
                    errors.Add($"Field '{field.Name}' of type '{field.Type}' must have options");
                }

                // Validate field-specific validation rules
                await ValidateFieldValidationRules(field, errors);
            }

            return errors;
        }

        public async Task<IEnumerable<string>> ValidateConditionalLogicAsync(FormSchema schema)
        {
            var errors = new List<string>();

            if (schema.ConditionalLogic == null || !schema.ConditionalLogic.Any())
            {
                return errors;
            }

            var fieldIds = schema.Fields.Select(f => f.Id).ToHashSet();

            foreach (var logic in schema.ConditionalLogic)
            {
                // Validate dependent fields exist
                if (logic.DependentFields != null)
                {
                    foreach (var fieldId in logic.DependentFields)
                    {
                        if (!fieldIds.Contains(fieldId))
                        {
                            errors.Add($"Conditional logic references non-existent dependent field: {fieldId}");
                        }
                    }
                }
            }

            return errors;
        }

        public async Task<IEnumerable<string>> ValidateFormulasAsync(FormSchema schema)
        {
            var errors = new List<string>();

            if (schema.Formulas == null || !schema.Formulas.Any())
            {
                return errors;
            }

            var fieldIds = schema.Fields.Select(f => f.Id).ToHashSet();

            foreach (var formula in schema.Formulas)
            {
                // Validate expression is not empty
                if (string.IsNullOrWhiteSpace(formula.Expression))
                {
                    errors.Add($"Formula expression cannot be empty");
                }

                // Basic expression validation (can be enhanced with actual expression parser)
                if (!IsValidExpression(formula.Expression))
                {
                    errors.Add($"Invalid formula expression: {formula.Expression}");
                }

                // Validate referenced fields in expression exist
                var referencedFields = ExtractFieldReferences(formula.Expression);
                foreach (var fieldRef in referencedFields)
                {
                    if (!fieldIds.Contains(fieldRef))
                    {
                        errors.Add($"Formula references non-existent field '{fieldRef}' in expression: {formula.Expression}");
                    }
                }
            }

            return errors;
        }

        public async Task<IEnumerable<string>> ValidateStructureAsync(FormSchema schema)
        {
            var errors = new List<string>();

            // Validate version
            if (string.IsNullOrWhiteSpace(schema.Version))
            {
                errors.Add("Schema version is required");
            }
            else if (!SupportedVersions.Contains(schema.Version))
            {
                errors.Add($"Unsupported schema version: {schema.Version}. Supported versions: {string.Join(", ", SupportedVersions)}");
            }

            // Validate title
            if (string.IsNullOrWhiteSpace(schema.Title))
            {
                errors.Add("Schema title is required");
            }

            // Validate fields exist
            if (schema.Fields == null || !schema.Fields.Any())
            {
                errors.Add("Schema must contain at least one field");
            }

            return errors;
        }

        public async Task<Result<bool>> ValidateVersionCompatibilityAsync(FormSchema schema, string targetVersion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(targetVersion))
                {
                    return Result<bool>.Failure("Target version cannot be null or empty");
                }

                if (!SupportedVersions.Contains(targetVersion))
                {
                    return Result<bool>.Failure($"Unsupported target version: {targetVersion}");
                }

                if (string.IsNullOrWhiteSpace(schema.Version))
                {
                    return Result<bool>.Failure("Schema version is required for compatibility check");
                }

                // Simple version compatibility check (can be enhanced based on actual versioning strategy)
                var schemaVersionIndex = Array.IndexOf(SupportedVersions, schema.Version);
                var targetVersionIndex = Array.IndexOf(SupportedVersions, targetVersion);

                if (schemaVersionIndex > targetVersionIndex)
                {
                    return Result<bool>.Failure($"Schema version {schema.Version} is not compatible with target version {targetVersion}");
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during version compatibility validation");
                return Result<bool>.Failure($"Version compatibility error: {ex.Message}");
            }
        }

        private async Task ValidateFieldValidationRules(FieldDefinition field, List<string> errors)
        {
            if (field.ValidationRules == null || !field.ValidationRules.Any())
            {
                return;
            }

            foreach (var rule in field.ValidationRules)
            {
                if (string.IsNullOrWhiteSpace(rule.Type))
                {
                    errors.Add($"Validation rule type is required for field: {field.Name}");
                }

                // Validate rule-specific requirements
                switch (rule.Type.ToLowerInvariant())
                {
                    case "minlength":
                    case "maxlength":
                        if (rule.Value == null || !int.TryParse(rule.Value.ToString(), out _))
                        {
                            errors.Add($"Validation rule '{rule.Type}' requires a numeric value for field: {field.Name}");
                        }
                        break;
                    case "pattern":
                        if (string.IsNullOrWhiteSpace(rule.Value?.ToString()))
                        {
                            errors.Add($"Validation rule 'pattern' requires a regex pattern for field: {field.Name}");
                        }
                        break;
                }
            }
        }

        /*
        private bool HasCircularDependency(ConditionalLogic logic, IEnumerable<ConditionalLogic> allLogic)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            return HasCircularDependencyRecursive(logic.TargetFieldId, allLogic, visited, recursionStack);
        }

        private bool HasCircularDependencyRecursive(string fieldId, IEnumerable<ConditionalLogic> allLogic, 
            HashSet<string> visited, HashSet<string> recursionStack)
        {
            visited.Add(fieldId);
            recursionStack.Add(fieldId);

            var dependentLogic = allLogic.Where(l => l.Conditions.Any(c => c.FieldId == fieldId));

            foreach (var logic in dependentLogic)
            {
                if (!visited.Contains(logic.TargetFieldId))
                {
                    if (HasCircularDependencyRecursive(logic.TargetFieldId, allLogic, visited, recursionStack))
                    {
                        return true;
                    }
                }
                else if (recursionStack.Contains(logic.TargetFieldId))
                {
                    return true;
                }
            }

            recursionStack.Remove(fieldId);
            return false;
        }
        */

        private bool IsValidExpression(string expression)
        {
            // Basic expression validation - can be enhanced with actual expression parser
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            // Check for balanced parentheses
            var openCount = expression.Count(c => c == '(');
            var closeCount = expression.Count(c => c == ')');
            
            return openCount == closeCount;
        }

        private IEnumerable<string> ExtractFieldReferences(string expression)
        {
            // Simple field reference extraction - can be enhanced with actual expression parser
            var fieldRefs = new List<string>();
            
            // Look for patterns like {fieldName} or [fieldName]
            var patterns = new[] { @"\{([^}]+)\}", @"\[([^\]]+)\]" };
            
            foreach (var pattern in patterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(expression, pattern);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        fieldRefs.Add(match.Groups[1].Value);
                    }
                }
            }

            return fieldRefs.Distinct();
        }
    }
}