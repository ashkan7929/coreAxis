using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Interface for managing dynamic options for form fields.
    /// Handles evaluation of dynamic options based on expressions and external data sources.
    /// </summary>
    public interface IDynamicOptionsManager
    {
        /// <summary>
        /// Evaluates dynamic options for a field based on an expression and form data.
        /// </summary>
        /// <param name="expression">The expression to evaluate for dynamic options.</param>
        /// <param name="formData">The current form data context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the evaluated field options.</returns>
        Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> EvaluateDynamicOptionsAsync(
            string expression,
            Dictionary<string, object?> formData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Evaluates dynamic options for multiple fields at once.
        /// </summary>
        /// <param name="fieldExpressions">Dictionary of field names and their dynamic option expressions.</param>
        /// <param name="formData">The current form data context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the evaluated options for each field.</returns>
        Task<CoreAxis.SharedKernel.Result<Dictionary<string, List<FieldOption>>>> EvaluateMultipleDynamicOptionsAsync(
            Dictionary<string, string> expressions,
            Dictionary<string, object?> formData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets options from an external API endpoint.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint configuration.</param>
        /// <param name="parameters">Parameters to pass to the API.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the options from the API.</returns>
        Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> GetOptionsFromApiAsync(
            string apiUrl,
            Dictionary<string, object?> parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets options from a database query.
        /// </summary>
        /// <param name="query">The database query configuration.</param>
        /// <param name="parameters">Parameters for the query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the options from the database.</returns>
        Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> GetOptionsFromDatabaseAsync(
            string query,
            Dictionary<string, object?> parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Filters existing options based on conditions.
        /// </summary>
        /// <param name="options">The original options to filter.</param>
        /// <param name="filterCondition">The filter condition.</param>
        /// <param name="formData">The current form data context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the filtered options.</returns>
        Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> FilterOptionsAsync(
            List<FieldOption> options,
            string filterCondition,
            Dictionary<string, object?> formData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a dynamic options expression is syntactically correct.
        /// </summary>
        /// <param name="expression">The expression to validate.</param>
        /// <returns>A result indicating if the expression is valid.</returns>
        Task<CoreAxis.SharedKernel.Result<bool>> ValidateDynamicOptionsExpression(string expression);

        /// <summary>
        /// Gets available functions that can be used in dynamic options expressions.
        /// </summary>
        /// <returns>A list of available function names and their descriptions.</returns>
        Task<CoreAxis.SharedKernel.Result<Dictionary<string, string>>> GetAvailableFunctions();
    }

    /// <summary>
    /// Configuration for API-based dynamic options.
    /// </summary>
    public class ApiOptionsConfig
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string ValueField { get; set; } = "value";
        public string LabelField { get; set; } = "label";
        public string? DescriptionField { get; set; }
        public string? GroupField { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
        public bool CacheResults { get; set; } = true;
        public int CacheDurationMinutes { get; set; } = 15;
    }

    /// <summary>
    /// Configuration for database-based dynamic options.
    /// </summary>
    public class DatabaseOptionsConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string ValueColumn { get; set; } = "value";
        public string LabelColumn { get; set; } = "label";
        public string? DescriptionColumn { get; set; }
        public string? GroupColumn { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
        public bool CacheResults { get; set; } = true;
        public int CacheDurationMinutes { get; set; } = 15;
    }
}