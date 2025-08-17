using CoreAxis.SharedKernel.DomainEvents;
using System;

namespace CoreAxis.Modules.DynamicForm.Domain.Events
{
    /// <summary>
    /// Event raised when a formula evaluation is started.
    /// </summary>
    public class FormulaEvaluationStartedEvent : DomainEvent
    {
        public Guid EvaluationLogId { get; }
        public Guid FormulaDefinitionId { get; }
        public string TenantId { get; }
        public Guid? ContextId { get; }
        public string ContextType { get; }

        public FormulaEvaluationStartedEvent(Guid evaluationLogId, Guid formulaDefinitionId, string tenantId, Guid? contextId, string contextType)
        {
            EvaluationLogId = evaluationLogId;
            FormulaDefinitionId = formulaDefinitionId;
            TenantId = tenantId;
            ContextId = contextId;
            ContextType = contextType;
        }
    }

    /// <summary>
    /// Event raised when a formula evaluation is completed successfully.
    /// </summary>
    public class FormulaEvaluationCompletedEvent : DomainEvent
    {
        public Guid EvaluationLogId { get; }
        public Guid FormulaDefinitionId { get; }
        public string TenantId { get; }
        public string Status { get; }
        public long ExecutionTimeMs { get; }

        public FormulaEvaluationCompletedEvent(Guid evaluationLogId, Guid formulaDefinitionId, string tenantId, string status, long executionTimeMs)
        {
            EvaluationLogId = evaluationLogId;
            FormulaDefinitionId = formulaDefinitionId;
            TenantId = tenantId;
            Status = status;
            ExecutionTimeMs = executionTimeMs;
        }
    }

    /// <summary>
    /// Event raised when a formula evaluation fails.
    /// </summary>
    public class FormulaEvaluationFailedEvent : DomainEvent
    {
        public Guid EvaluationLogId { get; }
        public Guid FormulaDefinitionId { get; }
        public string TenantId { get; }
        public string ErrorMessage { get; }

        public FormulaEvaluationFailedEvent(Guid evaluationLogId, Guid formulaDefinitionId, string tenantId, string errorMessage)
        {
            EvaluationLogId = evaluationLogId;
            FormulaDefinitionId = formulaDefinitionId;
            TenantId = tenantId;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Event raised when a formula evaluation times out.
    /// </summary>
    public class FormulaEvaluationTimedOutEvent : DomainEvent
    {
        public Guid EvaluationLogId { get; }
        public Guid FormulaDefinitionId { get; }
        public string TenantId { get; }
        public long TimeoutMs { get; }

        public FormulaEvaluationTimedOutEvent(Guid evaluationLogId, Guid formulaDefinitionId, string tenantId, long timeoutMs)
        {
            EvaluationLogId = evaluationLogId;
            FormulaDefinitionId = formulaDefinitionId;
            TenantId = tenantId;
            TimeoutMs = timeoutMs;
        }
    }
}