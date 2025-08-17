using CoreAxis.Modules.DynamicForm.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for FormulaEvaluationLog entity operations.
    /// </summary>
    public interface IFormulaEvaluationLogRepository
    {
        /// <summary>
        /// Gets a formula evaluation log by its identifier.
        /// </summary>
        /// <param name="id">The log identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The evaluation log if found; otherwise, null.</returns>
        Task<FormulaEvaluationLog> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a formula evaluation log by its identifier including formula definition.
        /// </summary>
        /// <param name="id">The log identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The evaluation log if found; otherwise, null.</returns>
        Task<FormulaEvaluationLog> GetByIdWithFormulaAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all evaluation logs for a specific formula definition.
        /// </summary>
        /// <param name="formulaDefinitionId">The formula definition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetByFormulaDefinitionIdAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation logs by context.
        /// </summary>
        /// <param name="contextId">The context identifier.</param>
        /// <param name="contextType">The context type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetByContextAsync(string contextId, string contextType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation logs by status.
        /// </summary>
        /// <param name="status">The evaluation status.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetByStatusAsync(string status, string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation logs by user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetByUserIdAsync(string userId, string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation logs by session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation logs by tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation logs by date range.
        /// </summary>
        /// <param name="fromDate">The start date.</param>
        /// <param name="toDate">The end date.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation logs with pagination support.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="formulaDefinitionId">Optional formula definition filter.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="userId">Optional user filter.</param>
        /// <param name="contextType">Optional context type filter.</param>
        /// <param name="fromDate">Optional start date filter.</param>
        /// <param name="toDate">Optional end date filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated result of evaluation logs.</returns>
        Task<(IEnumerable<FormulaEvaluationLog> Logs, int TotalCount)> GetPagedAsync(
            string tenantId,
            int pageNumber,
            int pageSize,
            Guid? formulaDefinitionId = null,
            string? status = null,
            string userId = null,
            string contextType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation logs by their identifiers.
        /// </summary>
        /// <param name="ids">The log identifiers.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets failed evaluation logs.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="fromDate">Optional start date filter.</param>
        /// <param name="toDate">Optional end date filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of failed evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetFailedEvaluationsAsync(string tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets timed out evaluation logs.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="fromDate">Optional start date filter.</param>
        /// <param name="toDate">Optional end date filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of timed out evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetTimedOutEvaluationsAsync(string tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets slow evaluation logs (above specified execution time threshold).
        /// </summary>
        /// <param name="thresholdMs">The execution time threshold in milliseconds.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="fromDate">Optional start date filter.</param>
        /// <param name="toDate">Optional end date filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of slow evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetSlowEvaluationsAsync(long thresholdMs, string tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation performance statistics.
        /// </summary>
        /// <param name="formulaDefinitionId">The formula definition identifier.</param>
        /// <param name="fromDate">Optional start date filter.</param>
        /// <param name="toDate">Optional end date filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Performance statistics including average, min, max execution times and success rate.</returns>
        Task<(double AvgExecutionTimeMs, long MinExecutionTimeMs, long MaxExecutionTimeMs, double SuccessRate, int TotalEvaluations)> GetPerformanceStatisticsAsync(
            Guid formulaDefinitionId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation statistics by tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="fromDate">Optional start date filter.</param>
        /// <param name="toDate">Optional end date filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Evaluation statistics by status.</returns>
        Task<Dictionary<string, int>> GetEvaluationStatisticsByTenantAsync(string tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of evaluation logs.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of evaluation logs.</returns>
        Task<int> GetCountAsync(string tenantId, string? status = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of evaluation logs by formula definition.
        /// </summary>
        /// <param name="formulaDefinitionId">The formula definition identifier.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of evaluation logs.</returns>
        Task<int> GetCountByFormulaAsync(Guid formulaDefinitionId, string? status = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets evaluation logs that have been modified since a specific date.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="since">The date to check modifications since.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of modified evaluation logs.</returns>
        Task<IEnumerable<FormulaEvaluationLog>> GetModifiedSinceAsync(string tenantId, DateTime since, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up old evaluation logs based on retention policy.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="retentionDays">The number of days to retain logs.</param>
        /// <param name="keepFailedLogs">Whether to keep failed logs regardless of retention period.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of deleted logs.</returns>
        Task<int> CleanupOldLogsAsync(string tenantId, int retentionDays, bool keepFailedLogs = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new evaluation log to the repository.
        /// </summary>
        /// <param name="evaluationLog">The evaluation log to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(FormulaEvaluationLog evaluationLog, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple evaluation logs to the repository.
        /// </summary>
        /// <param name="evaluationLogs">The evaluation logs to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddRangeAsync(IEnumerable<FormulaEvaluationLog> evaluationLogs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing evaluation log in the repository.
        /// </summary>
        /// <param name="evaluationLog">The evaluation log to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(FormulaEvaluationLog evaluationLog, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates multiple evaluation logs in the repository.
        /// </summary>
        /// <param name="evaluationLogs">The evaluation logs to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateRangeAsync(IEnumerable<FormulaEvaluationLog> evaluationLogs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an evaluation log from the repository.
        /// </summary>
        /// <param name="evaluationLog">The evaluation log to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveAsync(FormulaEvaluationLog evaluationLog, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an evaluation log by its identifier.
        /// </summary>
        /// <param name="id">The evaluation log identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes evaluation logs by formula definition identifier.
        /// </summary>
        /// <param name="formulaDefinitionId">The formula definition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveByFormulaDefinitionIdAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes to the repository.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of affected records.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}