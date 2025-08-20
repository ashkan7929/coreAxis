using System.Collections.Generic;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;

using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Interface for form schema validation service.
    /// </summary>
    public interface IFormSchemaValidator
    {
        /// <summary>
        /// Validates a form schema for consistency and completeness.
        /// </summary>
        /// <param name="schema">The form schema to validate.</param>
        /// <returns>A result containing validation errors if any.</returns>
        Task<Result<bool>> ValidateAsync(FormSchema schema);

        /// <summary>
        /// Validates a form schema from JSON string.
        /// </summary>
        /// <param name="schemaJson">The form schema JSON to validate.</param>
        /// <returns>A result containing validation errors if any.</returns>
        Task<Result<bool>> ValidateJsonAsync(string schemaJson);

        /// <summary>
        /// Validates field definitions within a schema.
        /// </summary>
        /// <param name="fields">The field definitions to validate.</param>
        /// <returns>A collection of validation errors.</returns>
        Task<IEnumerable<string>> ValidateFieldsAsync(IEnumerable<FieldDefinition> fields);

        /// <summary>
        /// Validates conditional logic references within a schema.
        /// </summary>
        /// <param name="schema">The form schema to validate.</param>
        /// <returns>A collection of validation errors.</returns>
        Task<IEnumerable<string>> ValidateConditionalLogicAsync(FormSchema schema);

        /// <summary>
        /// Validates formula expressions within a schema.
        /// </summary>
        /// <param name="schema">The form schema to validate.</param>
        /// <returns>A collection of validation errors.</returns>
        Task<IEnumerable<string>> ValidateFormulasAsync(FormSchema schema);

        /// <summary>
        /// Validates schema structure and required properties.
        /// </summary>
        /// <param name="schema">The form schema to validate.</param>
        /// <returns>A collection of validation errors.</returns>
        Task<IEnumerable<string>> ValidateStructureAsync(FormSchema schema);

        /// <summary>
        /// Validates schema against a specific version.
        /// </summary>
        /// <param name="schema">The form schema to validate.</param>
        /// <param name="targetVersion">The target schema version.</param>
        /// <returns>A result containing validation errors if any.</returns>
        Task<Result<bool>> ValidateVersionCompatibilityAsync(FormSchema schema, string targetVersion);
    }
}