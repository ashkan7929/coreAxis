using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Domain.Repositories
{
    /// <summary>
    /// Repository interface for managing form steps.
    /// </summary>
    public interface IFormStepRepository : IRepository<FormStep>
    {
        /// <summary>
        /// Gets a form step by ID with tenant filtering.
        /// </summary>
        /// <param name="id">The form step identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form step if found, otherwise null.</returns>
        Task<FormStep> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all steps for a specific form with tenant filtering.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form steps.</returns>
        Task<IEnumerable<FormStep>> GetByFormIdAsync(Guid formId, Guid tenantId, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets all steps for a specific form ordered by step number.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form steps.</returns>
        Task<IEnumerable<FormStep>> GetByFormIdAsync(Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific step by form ID and step number.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="stepNumber">The step number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form step if found, otherwise null.</returns>
        Task<FormStep> GetByFormIdAndStepNumberAsync(Guid formId, int stepNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the maximum step number for a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The maximum step number, or 0 if no steps exist.</returns>
        Task<int> GetMaxStepNumberAsync(Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets steps that have conditional logic dependencies on a specific field.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="fieldName">The field name to check dependencies for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of dependent form steps.</returns>
        Task<IEnumerable<FormStep>> GetStepsWithConditionalDependencyAsync(Guid formId, string fieldName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets steps that are required and cannot be skipped.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of required form steps.</returns>
        Task<IEnumerable<FormStep>> GetRequiredStepsAsync(Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets steps that can be skipped.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of skippable form steps.</returns>
        Task<IEnumerable<FormStep>> GetSkippableStepsAsync(Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets steps within a specific step number range.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="fromStepNumber">The starting step number (inclusive).</param>
        /// <param name="toStepNumber">The ending step number (inclusive).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form steps within the range.</returns>
        Task<IEnumerable<FormStep>> GetStepsInRangeAsync(Guid formId, int fromStepNumber, int toStepNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a step number already exists for a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="stepNumber">The step number to check.</param>
        /// <param name="excludeStepId">Optional step ID to exclude from the check (for updates).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the step number exists, otherwise false.</returns>
        Task<bool> StepNumberExistsAsync(Guid formId, int stepNumber, Guid? excludeStepId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets steps by tenant for multi-tenancy support.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form steps for the tenant.</returns>
        Task<IEnumerable<FormStep>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default);
    }
}