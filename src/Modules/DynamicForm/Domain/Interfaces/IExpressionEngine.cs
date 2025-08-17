using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Domain.Interfaces
{
    /// <summary>
    /// Interface for expression evaluation engine that provides secure evaluation of expressions.
    /// </summary>
    public interface IExpressionEngine
    {
        /// <summary>
        /// Evaluates a formula expression with the given context.
        /// </summary>
        /// <param name="expression">The formula expression to evaluate.</param>
        /// <param name="context">The evaluation context containing variables and their values.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The evaluation result.</returns>
        Task<ExpressionEvaluationResult> EvaluateAsync(
            FormulaExpression expression,
            ExpressionEvaluationContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Evaluates a conditional logic expression.
        /// </summary>
        /// <param name="conditionalLogic">The conditional logic to evaluate.</param>
        /// <param name="context">The evaluation context containing variables and their values.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The evaluation result indicating whether the condition is met.</returns>
        Task<bool> EvaluateConditionalAsync(
            ConditionalLogic conditionalLogic,
            ExpressionEvaluationContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an expression for syntax and security issues.
        /// </summary>
        /// <param name="expression">The expression to validate.</param>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>The validation result.</returns>
        ExpressionValidationResult ValidateExpression(
            string expression,
            ValidationContext validationContext = null);

        /// <summary>
        /// Gets the available functions that can be used in expressions.
        /// </summary>
        /// <returns>A collection of available function signatures.</returns>
        IEnumerable<FunctionSignature> GetAvailableFunctions();

        /// <summary>
        /// Gets the supported operators in expressions.
        /// </summary>
        /// <returns>A collection of supported operators.</returns>
        IEnumerable<string> GetSupportedOperators();

        /// <summary>
        /// Checks if an expression is safe to execute (no security risks).
        /// </summary>
        /// <param name="expression">The expression to check.</param>
        /// <returns>True if the expression is safe; otherwise, false.</returns>
        bool IsSafeExpression(string expression);
    }

    /// <summary>
    /// Represents the result of an expression evaluation.
    /// </summary>
    public class ExpressionEvaluationResult
    {
        /// <summary>
        /// Gets or sets the evaluation result value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the type of the result value.
        /// </summary>
        public Type ValueType { get; set; }

        /// <summary>
        /// Gets or sets whether the evaluation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the error message if evaluation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the evaluation.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a successful evaluation result.
        /// </summary>
        /// <param name="value">The result value.</param>
        /// <param name="valueType">The type of the result value.</param>
        /// <param name="executionTimeMs">The execution time in milliseconds.</param>
        /// <returns>A successful evaluation result.</returns>
        public static ExpressionEvaluationResult Success(object value, Type valueType, long executionTimeMs = 0)
        {
            return new ExpressionEvaluationResult
            {
                Value = value,
                ValueType = valueType,
                IsSuccess = true,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Creates a failed evaluation result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="executionTimeMs">The execution time in milliseconds.</param>
        /// <returns>A failed evaluation result.</returns>
        public static ExpressionEvaluationResult Failure(string errorMessage, long executionTimeMs = 0)
        {
            return new ExpressionEvaluationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ExecutionTimeMs = executionTimeMs
            };
        }
    }

    /// <summary>
    /// Represents the context for expression evaluation.
    /// </summary>
    public class ExpressionEvaluationContext
    {
        /// <summary>
        /// Gets or sets the variables and their values.
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the form data context.
        /// </summary>
        public Dictionary<string, object> FormData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the user context information.
        /// </summary>
        public Dictionary<string, object> UserContext { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time allowed for the expression.
        /// </summary>
        public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets whether to enable debug mode for detailed evaluation information.
        /// </summary>
        public bool DebugMode { get; set; } = false;

        /// <summary>
        /// Adds a variable to the context.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="value">The variable value.</param>
        public void AddVariable(string name, object value)
        {
            Variables[name] = value;
        }

        /// <summary>
        /// Gets a variable value from the context.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <returns>The variable value if found; otherwise, null.</returns>
        public object GetVariable(string name)
        {
            return Variables.TryGetValue(name, out var value) ? value : null;
        }
    }

    /// <summary>
    /// Represents the result of expression validation.
    /// </summary>
    public class ExpressionValidationResult
    {
        /// <summary>
        /// Gets or sets whether the expression is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the security issues found in the expression.
        /// </summary>
        public List<string> SecurityIssues { get; set; } = new List<string>();

        /// <summary>
        /// Creates a valid expression validation result.
        /// </summary>
        /// <returns>A valid validation result.</returns>
        public static ExpressionValidationResult Valid()
        {
            return new ExpressionValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates an invalid expression validation result.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        /// <returns>An invalid validation result.</returns>
        public static ExpressionValidationResult Invalid(params string[] errors)
        {
            return new ExpressionValidationResult
            {
                IsValid = false,
                Errors = new List<string>(errors)
            };
        }
    }
}