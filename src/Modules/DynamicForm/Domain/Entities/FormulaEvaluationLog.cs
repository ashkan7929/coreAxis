using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using CoreAxis.Modules.DynamicForm.Domain.Events;
using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents a log entry for formula evaluation.
    /// </summary>
    public class FormulaEvaluationLog : EntityBase
    {
        /// <summary>
        /// Gets or sets the formula definition identifier.
        /// </summary>
        [Required]
        public Guid FormulaDefinitionId { get; set; }

        /// <summary>
        /// Gets or sets the formula version identifier.
        /// </summary>
        public Guid? FormulaVersionId { get; set; }

        /// <summary>
        /// Gets or sets the context identifier (e.g., form submission ID, user ID, etc.).
        /// </summary>
        public Guid? ContextId { get; set; }

        /// <summary>
        /// Gets or sets the context type (e.g., "FormSubmission", "BatchEvaluation", etc.).
        /// </summary>
        [MaxLength(100)]
        public string ContextType { get; set; }

        /// <summary>
        /// Gets or sets the input parameters used for evaluation as JSON.
        /// </summary>
        public string InputParameters { get; set; }

        /// <summary>
        /// Gets or sets the result of the formula evaluation as JSON.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets the status of the evaluation.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = EvaluationStatus.Success;

        /// <summary>
        /// Gets or sets the error message if the evaluation failed.
        /// </summary>
        [MaxLength(2000)]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the error details as JSON (stack trace, inner exceptions, etc.).
        /// </summary>
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the evaluation started.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the evaluation completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets a value indicating whether the evaluation was successful.
        /// </summary>
        public bool IsSuccess => Status == EvaluationStatus.Success;

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who triggered the evaluation.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the session identifier for tracking related evaluations.
        /// </summary>
        [MaxLength(100)]
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the evaluation mode (e.g., "Single", "Batch", "Test").
        /// </summary>
        [MaxLength(50)]
        public string EvaluationMode { get; set; } = "Single";

        /// <summary>
        /// Gets or sets additional metadata for the evaluation as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the IP address from which the evaluation was triggered.
        /// </summary>
        [MaxLength(45)] // IPv6 max length
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent string of the client that triggered the evaluation.
        /// </summary>
        [MaxLength(500)]
        public string UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the formula definition.
        /// </summary>
        public virtual FormulaDefinition FormulaDefinition { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the formula version.
        /// </summary>
        public virtual FormulaVersion? FormulaVersion { get; set; }

        /// <summary>
        /// Gets or sets the form identifier (optional).
        /// </summary>
        public Guid? FormId { get; set; }

        /// <summary>
        /// Gets or sets the form submission identifier (optional).
        /// </summary>
        public Guid? FormSubmissionId { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the form submission.
        /// </summary>
        public virtual FormSubmission? FormSubmission { get; set; }

        /// <summary>
        /// Gets or sets the evaluation context information.
        /// </summary>
        [MaxLength(500)]
        public string EvaluationContext { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier for tracking related operations.
        /// </summary>
        public Guid? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the form navigation property (optional).
        /// </summary>
        public virtual Form? Form { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaEvaluationLog"/> class.
        /// </summary>
        protected FormulaEvaluationLog()
        {
            // Required for EF Core
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaEvaluationLog"/> class.
        /// </summary>
        /// <param name="formulaDefinitionId">The formula definition identifier.</param>
        /// <param name="formulaVersionId">The formula version identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="inputParameters">The input parameters as JSON.</param>
        /// <param name="contextId">The context identifier.</param>
        /// <param name="contextType">The context type.</param>
        /// <param name="userId">The user identifier.</param>
        public FormulaEvaluationLog(
            Guid formulaDefinitionId,
            Guid? formulaVersionId,
            string tenantId,
            string inputParameters,
            Guid? contextId = null,
            string contextType = null,
            Guid? userId = null)
        {
            if (formulaDefinitionId == Guid.Empty)
                throw new ArgumentException("Formula definition ID cannot be empty.", nameof(formulaDefinitionId));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

            Id = Guid.NewGuid();
            FormulaDefinitionId = formulaDefinitionId;
            FormulaVersionId = formulaVersionId;
            TenantId = tenantId;
            InputParameters = inputParameters;
            ContextId = contextId;
            ContextType = contextType;
            UserId = userId;
            StartedAt = DateTime.UtcNow;
            CreatedBy = userId?.ToString() ?? "System";
            CreatedOn = DateTime.UtcNow;
            IsActive = true;

            AddDomainEvent(new FormulaEvaluationStartedEvent(Id, FormulaDefinitionId, TenantId, ContextId, ContextType));
        }

        /// <summary>
        /// Marks the evaluation as successful and sets the result.
        /// </summary>
        /// <param name="result">The evaluation result as JSON.</param>
        /// <param name="executionTimeMs">The execution time in milliseconds.</param>
        /// <param name="memoryUsageBytes">The memory usage in bytes.</param>
        public void MarkAsSuccess(string result, long executionTimeMs, long memoryUsageBytes = 0)
        {
            if (Status != EvaluationStatus.InProgress && Status != EvaluationStatus.Success)
                throw new InvalidOperationException($"Cannot mark evaluation as success when status is {Status}.");

            Status = EvaluationStatus.Success;
            Result = result;
            ExecutionTimeMs = executionTimeMs;
            MemoryUsageBytes = memoryUsageBytes;
            CompletedAt = DateTime.UtcNow;
            LastModifiedBy = CreatedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormulaEvaluationCompletedEvent(Id, FormulaDefinitionId, TenantId, Status, ExecutionTimeMs));
        }

        /// <summary>
        /// Marks the evaluation as failed and sets the error information.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errorDetails">The error details as JSON.</param>
        /// <param name="executionTimeMs">The execution time in milliseconds.</param>
        public void MarkAsFailed(string errorMessage, string errorDetails = null, long executionTimeMs = 0)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));

            Status = EvaluationStatus.Failed;
            ErrorMessage = errorMessage;
            ErrorDetails = errorDetails;
            ExecutionTimeMs = executionTimeMs;
            CompletedAt = DateTime.UtcNow;
            LastModifiedBy = CreatedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormulaEvaluationFailedEvent(Id, FormulaDefinitionId, TenantId, errorMessage));
        }

        /// <summary>
        /// Marks the evaluation as timed out.
        /// </summary>
        /// <param name="timeoutMs">The timeout value in milliseconds.</param>
        public void MarkAsTimedOut(long timeoutMs)
        {
            Status = EvaluationStatus.TimedOut;
            ErrorMessage = $"Formula evaluation timed out after {timeoutMs}ms";
            ExecutionTimeMs = timeoutMs;
            CompletedAt = DateTime.UtcNow;
            LastModifiedBy = CreatedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormulaEvaluationTimedOutEvent(Id, FormulaDefinitionId, TenantId, timeoutMs));
        }

        /// <summary>
        /// Sets the evaluation context information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="evaluationMode">The evaluation mode.</param>
        /// <param name="ipAddress">The IP address.</param>
        /// <param name="userAgent">The user agent.</param>
        public void SetContext(string sessionId, string evaluationMode, string ipAddress = null, string userAgent = null)
        {
            SessionId = sessionId;
            EvaluationMode = evaluationMode;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            LastModifiedBy = CreatedBy;
            LastModifiedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets additional metadata for the evaluation.
        /// </summary>
        /// <param name="metadata">The metadata as JSON.</param>
        public void SetMetadata(string metadata)
        {
            Metadata = metadata;
            LastModifiedBy = CreatedBy;
            LastModifiedOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Constants for evaluation status values.
    /// </summary>
    public static class EvaluationStatus
    {
        public const string InProgress = "InProgress";
        public const string Success = "Success";
        public const string Failed = "Failed";
        public const string TimedOut = "TimedOut";
        public const string Cancelled = "Cancelled";
    }

    /// <summary>
    /// Constants for evaluation mode values.
    /// </summary>
    public static class EvaluationMode
    {
        public const string Single = "Single";
        public const string Batch = "Batch";
        public const string Test = "Test";
        public const string Validation = "Validation";
        public const string Performance = "Performance";
    }


}