using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Domain.Repositories
{
    /// <summary>
    /// Repository interface for managing form step submissions.
    /// </summary>
    public interface IFormStepSubmissionRepository : IRepository<FormStepSubmission>
    {
        /// <summary>
        /// Gets a form step submission by ID with tenant filtering.
        /// </summary>
        /// <param name="id">The form step submission identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form step submission if found, otherwise null.</returns>
        Task<FormStepSubmission> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all step submissions for a specific form submission with tenant filtering.
        /// </summary>
        /// <param name="formSubmissionId">The form submission identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form step submissions.</returns>
        Task<IEnumerable<FormStepSubmission>> GetByFormSubmissionIdAsync(Guid formSubmissionId, Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific step submission by form submission ID and step number with tenant filtering.
        /// </summary>
        /// <param name="formSubmissionId">The form submission identifier.</param>
        /// <param name="stepNumber">The step number.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form step submission if found, otherwise null.</returns>
        Task<FormStepSubmission> GetByFormSubmissionIdAndStepNumberAsync(Guid formSubmissionId, int stepNumber, Guid tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets analytics data for form step submissions.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="startDate">The start date for analytics.</param>
        /// <param name="endDate">The end date for analytics.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of analytics data.</returns>
        Task<IEnumerable<dynamic>> GetAnalyticsAsync(Guid formId, Guid tenantId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
        /// <summary>
        /// Gets all step submissions for a specific form submission ordered by step number (legacy method).
        /// </summary>
        /// <param name="formSubmissionId">The form submission identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form step submissions.</returns>
        Task<IEnumerable<FormStepSubmission>> GetByFormSubmissionIdAsync(Guid formSubmissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific step submission by form submission ID and step number (legacy method).
        /// </summary>
        /// <param name="formSubmissionId">The form submission identifier.</param>
        /// <param name="stepNumber">The step number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form step submission if found, otherwise null.</returns>
        Task<FormStepSubmission> GetByFormSubmissionIdAndStepNumberAsync(Guid formSubmissionId, int stepNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets step submissions by form step ID.
        /// </summary>
        /// <param name="formStepId">The form step identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form step submissions.</returns>
        Task<IEnumerable<FormStepSubmission>> GetByFormStepIdAsync(Guid formStepId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets step submissions by user ID.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form step submissions.</returns>
        Task<IEnumerable<FormStepSubmission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets step submissions by status.
        /// </summary>
        /// <param name="status">The submission status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form step submissions with the specified status.</returns>
        Task<IEnumerable<FormStepSubmission>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets incomplete step submissions for a specific form submission.
        /// </summary>
        /// <param name="formSubmissionId">The form submission identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of incomplete form step submissions.</returns>
        Task<IEnumerable<FormStepSubmission>> GetIncompleteStepSubmissionsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets completed step submissions for a specific form submission.
        /// </summary>
        /// <param name="formSubmissionId">The form submission identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of completed form step submissions.</returns>
        Task<IEnumerable<FormStepSubmission>> GetCompletedStepSubmissionsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets skipped step submissions for a specific form submission.
        /// </summary>
        /// <param name="formSubmissionId">The form submission identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of skipped form step submissions.</returns>
        Task<IEnumerable<FormStepSubmission>> GetSkippedStepSubmissionsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets step submissions with validation errors.
        /// </summary>
        /// <param name="formSubmissionId">The form submission identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form step submissions with validation errors.</returns>
        Task<IEnumerable<FormStepSubmission>> GetStepSubmissionsWithErrorsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current step submission for a user's form submission.
        /// </summary>
        /// <param name="formSubmissionId">The form submission identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current step submission if found, otherwise null.</returns>
        Task<FormStepSubmission> GetCurrentStepSubmissionAsync(Guid formSubmissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets step submissions within a specific time range.
        /// </summary>
        /// <param name="fromDate">The start date (inclusive).</param>
        /// <param name="toDate">The end date (inclusive).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form step submissions within the time range.</returns>
        Task<IEnumerable<FormStepSubmission>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets step submissions that took longer than a specified time.
        /// </summary>
        /// <param name="minTimeSeconds">The minimum time in seconds.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form step submissions that exceeded the time limit.</returns>
        Task<IEnumerable<FormStepSubmission>> GetSlowStepSubmissionsAsync(int minTimeSeconds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets step submissions by tenant for multi-tenancy support.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of form step submissions for the tenant.</returns>
        Task<IEnumerable<FormStepSubmission>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets analytics data for step completion times.
        /// </summary>
        /// <param name="formStepId">The form step identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Analytics data including average, min, max completion times.</returns>
        Task<StepCompletionAnalytics> GetStepCompletionAnalyticsAsync(Guid formStepId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Analytics data for step completion times.
    /// </summary>
    public class StepCompletionAnalytics
    {
        public double AverageCompletionTimeSeconds { get; set; }
        public int MinCompletionTimeSeconds { get; set; }
        public int MaxCompletionTimeSeconds { get; set; }
        public int TotalSubmissions { get; set; }
        public int CompletedSubmissions { get; set; }
        public int SkippedSubmissions { get; set; }
        public double CompletionRate { get; set; }
        public double SkipRate { get; set; }
    }
}