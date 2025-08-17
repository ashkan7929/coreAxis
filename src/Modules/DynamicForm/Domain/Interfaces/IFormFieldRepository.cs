using CoreAxis.Modules.DynamicForm.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for FormField entity operations.
    /// </summary>
    public interface IFormFieldRepository
    {
        /// <summary>
        /// Gets a form field by its identifier.
        /// </summary>
        /// <param name="id">The form field identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form field if found; otherwise, null.</returns>
        Task<FormField> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a form field by its identifier including the related form.
        /// </summary>
        /// <param name="id">The form field identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form field if found; otherwise, null.</returns>
        Task<FormField> GetByIdWithFormAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all form fields for a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="includeInactive">Whether to include inactive fields.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form fields ordered by their order property.</returns>
        Task<IEnumerable<FormField>> GetByFormIdAsync(Guid formId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a form field by its name within a specific form.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form field if found; otherwise, null.</returns>
        Task<FormField> GetByNameAsync(string name, Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form fields by their identifiers.
        /// </summary>
        /// <param name="ids">The form field identifiers.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form fields.</returns>
        Task<IEnumerable<FormField>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form fields by field type.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="fieldType">The field type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form fields.</returns>
        Task<IEnumerable<FormField>> GetByFieldTypeAsync(Guid formId, string fieldType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets required form fields for a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of required form fields.</returns>
        Task<IEnumerable<FormField>> GetRequiredFieldsAsync(Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets calculated form fields for a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of calculated form fields.</returns>
        Task<IEnumerable<FormField>> GetCalculatedFieldsAsync(Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form fields with conditional logic for a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form fields with conditional logic.</returns>
        Task<IEnumerable<FormField>> GetFieldsWithConditionalLogicAsync(Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the maximum order value for fields in a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The maximum order value, or 0 if no fields exist.</returns>
        Task<int> GetMaxOrderAsync(Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a form field with the specified name exists within a form.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="formId">The form identifier.</param>
        /// <param name="excludeId">Optional field ID to exclude from the check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the field exists; otherwise, false.</returns>
        Task<bool> ExistsAsync(string name, Guid formId, Guid? excludeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of form fields for a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="includeInactive">Whether to include inactive fields.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of form fields.</returns>
        Task<int> GetCountAsync(Guid formId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form fields that have been modified since a specific date.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="since">The date to check modifications since.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of modified form fields.</returns>
        Task<IEnumerable<FormField>> GetModifiedSinceAsync(Guid formId, DateTime since, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reorders form fields within a form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="fieldOrders">A dictionary mapping field IDs to their new order values.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ReorderFieldsAsync(Guid formId, Dictionary<Guid, int> fieldOrders, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new form field to the repository.
        /// </summary>
        /// <param name="formField">The form field to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(FormField formField, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple form fields to the repository.
        /// </summary>
        /// <param name="formFields">The form fields to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddRangeAsync(IEnumerable<FormField> formFields, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing form field in the repository.
        /// </summary>
        /// <param name="formField">The form field to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(FormField formField, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates multiple form fields in the repository.
        /// </summary>
        /// <param name="formFields">The form fields to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateRangeAsync(IEnumerable<FormField> formFields, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a form field from the repository.
        /// </summary>
        /// <param name="formField">The form field to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveAsync(FormField formField, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a form field by its identifier.
        /// </summary>
        /// <param name="id">The form field identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all form fields for a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveByFormIdAsync(Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes to the repository.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of affected records.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}