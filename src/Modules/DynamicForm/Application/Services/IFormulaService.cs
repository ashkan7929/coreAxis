using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.SharedKernel.Common;
using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Interface for formula service that provides formula evaluation and management capabilities.
    /// </summary>
    public interface IFormulaService
    {
        /// <summary>
        /// Evaluates a formula with the given inputs.
        /// </summary>
        /// <param name="formulaId">The formula definition identifier.</param>
        /// <param name="inputs">The input variables for formula evaluation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The evaluation result containing value, formula version, and metadata.</returns>
        Task<Result<FormulaEvaluationResult>> EvaluateFormulaAsync(
            Guid formulaId,
            Dictionary<string, object> inputs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Evaluates a formula by name within a tenant.
        /// </summary>
        /// <param name="formulaName">The formula name.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="inputs">The input variables for formula evaluation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The evaluation result containing value, formula version, and metadata.</returns>
        Task<Result<FormulaEvaluationResult>> EvaluateFormulaByNameAsync(
            string formulaName,
            Guid tenantId,
            Dictionary<string, object> inputs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Evaluates a specific version of a formula.
        /// </summary>
        /// <param name="formulaId">The formula definition identifier.</param>
        /// <param name="version">The formula version number.</param>
        /// <param name="inputs">The input variables for formula evaluation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The evaluation result containing value, formula version, and metadata.</returns>
        Task<Result<FormulaEvaluationResult>> EvaluateFormulaVersionAsync(
            Guid formulaId,
            int version,
            Dictionary<string, object> inputs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest published version of a formula.
        /// </summary>
        /// <param name="formulaId">The formula definition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The latest published formula version.</returns>
        Task<Result<FormulaVersion>> GetLatestPublishedVersionAsync(
            Guid formulaId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available functions that can be used in formulas.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of available function signatures.</returns>
        Task<Result<IEnumerable<FunctionSignature>>> GetAvailableFunctionsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a formula expression syntax.
        /// </summary>
        /// <param name="expression">The formula expression to validate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The validation result.</returns>
        Task<Result<bool>> ValidateExpressionAsync(
            string expression,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets formula evaluation history for a specific formula.
        /// </summary>
        /// <param name="formulaId">The formula definition identifier.</param>
        /// <param name="pageNumber">The page number for pagination.</param>
        /// <param name="pageSize">The page size for pagination.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The paginated evaluation history.</returns>
        Task<Result<PaginatedList<FormulaEvaluationLog>>> GetEvaluationHistoryAsync(
            Guid formulaId,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets performance metrics for a formula.
        /// </summary>
        /// <param name="formulaId">The formula definition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The performance metrics.</returns>
        Task<Result<FormulaPerformanceMetrics>> GetPerformanceMetricsAsync(
            Guid formulaId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of formula evaluation containing value, metadata, and performance information.
    /// </summary>
    public class FormulaEvaluationResult
    {
        /// <summary>
        /// Gets or sets the evaluated value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the formula version used for evaluation.
        /// </summary>
        public int FormulaVersion { get; set; }

        /// <summary>
        /// Gets or sets the evaluation metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets the evaluation duration in milliseconds.
        /// </summary>
        public long EvaluationDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the evaluation log identifier.
        /// </summary>
        public Guid EvaluationLogId { get; set; }

        /// <summary>
        /// Gets or sets whether the evaluation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the error message if evaluation failed.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Performance metrics for formula evaluation.
    /// </summary>
    public class FormulaPerformanceMetrics
    {
        /// <summary>
        /// Gets or sets the total number of evaluations.
        /// </summary>
        public long TotalEvaluations { get; set; }

        /// <summary>
        /// Gets or sets the average evaluation time in milliseconds.
        /// </summary>
        public double AverageEvaluationTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the minimum evaluation time in milliseconds.
        /// </summary>
        public long MinEvaluationTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum evaluation time in milliseconds.
        /// </summary>
        public long MaxEvaluationTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the success rate as a percentage.
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets the number of failed evaluations.
        /// </summary>
        public long FailedEvaluations { get; set; }

        /// <summary>
        /// Gets or sets the last evaluation timestamp.
        /// </summary>
        public DateTime LastEvaluationAt { get; set; }
    }
}