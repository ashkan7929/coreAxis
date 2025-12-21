using CoreAxis.Modules.DynamicForm.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for FormSubmission entity operations.
    /// </summary>
    public interface IFormSubmissionRepository
    {
        /// <summary>
        /// Gets a form submission by its identifier.
        /// </summary>
        /// <param name="id">The form submission identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form submission if found; otherwise, null.</returns>
        Task<FormSubmission> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a form submission by its identifier including the related form.
        /// </summary>
        /// <param name="id">The form submission identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form submission if found; otherwise, null.</returns>
        Task<FormSubmission> GetByIdWithFormAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all form submissions for a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="includeInactive">Whether to include inactive submissions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form submissions ordered by submission date.</returns>
        Task<IEnumerable<FormSubmission>> GetByFormIdAsync(Guid formId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all form submissions for a specific user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="includeInactive">Whether to include inactive submissions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form submissions ordered by submission date.</returns>
        Task<IEnumerable<FormSubmission>> GetByUserIdAsync(Guid userId, string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all form submissions for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="includeInactive">Whether to include inactive submissions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form submissions ordered by submission date.</returns>
        Task<IEnumerable<FormSubmission>> GetByTenantAsync(string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form submissions by status.
        /// </summary>
        /// <param name="status">The submission status.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form submissions.</returns>
        Task<IEnumerable<FormSubmission>> GetByStatusAsync(string status, string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form submissions with pagination support.
        /// </summary>
        /// <param name="formId">The form identifier (optional).</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="userId">Optional user filter.</param>
        /// <param name="fromDate">Optional start date filter.</param>
        /// <param name="toDate">Optional end date filter.</param>
        /// <param name="includeInactive">Whether to include inactive submissions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated result of form submissions.</returns>
        Task<(IEnumerable<FormSubmission> Submissions, int TotalCount)> GetPagedAsync(
            string tenantId,
            int pageNumber,
            int pageSize,
            Guid? formId = null,
            string status = null,
            Guid? userId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            bool includeInactive = false,
            bool includeForm = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form submissions by their identifiers.
        /// </summary>
        /// <param name="ids">The form submission identifiers.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form submissions.</returns>
        Task<IEnumerable<FormSubmission>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets pending form submissions (draft or submitted status).
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of pending form submissions.</returns>
        Task<IEnumerable<FormSubmission>> GetPendingSubmissionsAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form submissions that need approval.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form submissions awaiting approval.</returns>
        Task<IEnumerable<FormSubmission>> GetSubmissionsAwaitingApprovalAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form submissions with validation errors.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form submissions with validation errors.</returns>
        Task<IEnumerable<FormSubmission>> GetSubmissionsWithErrorsAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form submissions within a date range.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="fromDate">The start date.</param>
        /// <param name="toDate">The end date.</param>
        /// <param name="formId">Optional form identifier filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of form submissions.</returns>
        Task<IEnumerable<FormSubmission>> GetByDateRangeAsync(
            string tenantId,
            DateTime fromDate,
            DateTime toDate,
            Guid? formId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of form submissions for a specific form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="includeInactive">Whether to include inactive submissions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of form submissions.</returns>
        Task<int> GetCountAsync(Guid formId, string status = null, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of form submissions for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="includeInactive">Whether to include inactive submissions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of form submissions.</returns>
        Task<int> GetCountByTenantAsync(string tenantId, string status = null, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets form submissions that have been modified since a specific date.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="since">The date to check modifications since.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of modified form submissions.</returns>
        Task<IEnumerable<FormSubmission>> GetModifiedSinceAsync(string tenantId, DateTime since, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets submission statistics for a form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A dictionary containing submission statistics by status.</returns>
        Task<Dictionary<string, int>> GetSubmissionStatisticsAsync(Guid formId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets submission statistics for a tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A dictionary containing submission statistics by status.</returns>
        Task<Dictionary<string, int>> GetSubmissionStatisticsByTenantAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new form submission to the repository.
        /// </summary>
        /// <param name="formSubmission">The form submission to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(FormSubmission formSubmission, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple form submissions to the repository.
        /// </summary>
        /// <param name="formSubmissions">The form submissions to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddRangeAsync(IEnumerable<FormSubmission> formSubmissions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing form submission in the repository.
        /// </summary>
        /// <param name="formSubmission">The form submission to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(FormSubmission formSubmission, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates multiple form submissions in the repository.
        /// </summary>
        /// <param name="formSubmissions">The form submissions to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateRangeAsync(IEnumerable<FormSubmission> formSubmissions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a form submission from the repository.
        /// </summary>
        /// <param name="formSubmission">The form submission to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveAsync(FormSubmission formSubmission, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a form submission by its identifier.
        /// </summary>
        /// <param name="id">The form submission identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all form submissions for a specific form.
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

        /// <summary>
        /// Gets submission statistics for a form.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="fromDate">Optional start date filter.</param>
        /// <param name="toDate">Optional end date filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Submission statistics.</returns>
        Task<ValueObjects.SubmissionStats> GetStatsAsync(Guid formId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    }
}