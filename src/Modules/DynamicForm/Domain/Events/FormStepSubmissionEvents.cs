using CoreAxis.SharedKernel.DomainEvents;
using System;

namespace CoreAxis.Modules.DynamicForm.Domain.Events
{
    /// <summary>
    /// Event raised when a form step submission is started.
    /// </summary>
    public class FormStepSubmissionStartedEvent : DomainEvent
    {
        public Guid StepSubmissionId { get; }
        public Guid FormSubmissionId { get; }
        public Guid FormStepId { get; }
        public int StepNumber { get; }
        public string TenantId { get; }
        public Guid UserId { get; }
        public string StartedBy { get; }

        public FormStepSubmissionStartedEvent(Guid stepSubmissionId, Guid formSubmissionId, Guid formStepId, int stepNumber, string tenantId, Guid userId, string startedBy)
        {
            StepSubmissionId = stepSubmissionId;
            FormSubmissionId = formSubmissionId;
            FormStepId = formStepId;
            StepNumber = stepNumber;
            TenantId = tenantId;
            UserId = userId;
            StartedBy = startedBy;
        }
    }

    /// <summary>
    /// Event raised when a form step submission is updated.
    /// </summary>
    public class FormStepSubmissionUpdatedEvent : DomainEvent
    {
        public Guid StepSubmissionId { get; }
        public Guid FormSubmissionId { get; }
        public Guid FormStepId { get; }
        public int StepNumber { get; }
        public string StepData { get; }
        public string UpdatedBy { get; }

        public FormStepSubmissionUpdatedEvent(Guid stepSubmissionId, Guid formSubmissionId, Guid formStepId, int stepNumber, string stepData, string updatedBy)
        {
            StepSubmissionId = stepSubmissionId;
            FormSubmissionId = formSubmissionId;
            FormStepId = formStepId;
            StepNumber = stepNumber;
            StepData = stepData;
            UpdatedBy = updatedBy;
        }
    }

    /// <summary>
    /// Event raised when a form step submission is completed.
    /// </summary>
    public class FormStepSubmissionCompletedEvent : DomainEvent
    {
        public Guid StepSubmissionId { get; }
        public Guid FormSubmissionId { get; }
        public Guid FormStepId { get; }
        public int StepNumber { get; }
        public string TenantId { get; }
        public Guid UserId { get; }
        public DateTime CompletedAt { get; }
        public int TimeSpentSeconds { get; }
        public string CompletedBy { get; }

        public FormStepSubmissionCompletedEvent(Guid stepSubmissionId, Guid formSubmissionId, Guid formStepId, int stepNumber, string tenantId, Guid userId, DateTime completedAt, int timeSpentSeconds, string completedBy)
        {
            StepSubmissionId = stepSubmissionId;
            FormSubmissionId = formSubmissionId;
            FormStepId = formStepId;
            StepNumber = stepNumber;
            TenantId = tenantId;
            UserId = userId;
            CompletedAt = completedAt;
            TimeSpentSeconds = timeSpentSeconds;
            CompletedBy = completedBy;
        }
    }

    /// <summary>
    /// Event raised when a form step submission is skipped.
    /// </summary>
    public class FormStepSubmissionSkippedEvent : DomainEvent
    {
        public Guid StepSubmissionId { get; }
        public Guid FormSubmissionId { get; }
        public Guid FormStepId { get; }
        public int StepNumber { get; }
        public string TenantId { get; }
        public Guid UserId { get; }
        public string SkipReason { get; }
        public string SkippedBy { get; }

        public FormStepSubmissionSkippedEvent(Guid stepSubmissionId, Guid formSubmissionId, Guid formStepId, int stepNumber, string tenantId, Guid userId, string skipReason, string skippedBy)
        {
            StepSubmissionId = stepSubmissionId;
            FormSubmissionId = formSubmissionId;
            FormStepId = formStepId;
            StepNumber = stepNumber;
            TenantId = tenantId;
            UserId = userId;
            SkipReason = skipReason;
            SkippedBy = skippedBy;
        }
    }

    /// <summary>
    /// Event raised when validation errors are set for a form step submission.
    /// </summary>
    public class FormStepSubmissionValidationErrorsSetEvent : DomainEvent
    {
        public Guid StepSubmissionId { get; }
        public Guid FormSubmissionId { get; }
        public Guid FormStepId { get; }
        public string ValidationErrors { get; }
        public int ErrorCount { get; }
        public string SetBy { get; }

        public FormStepSubmissionValidationErrorsSetEvent(Guid stepSubmissionId, Guid formSubmissionId, Guid formStepId, string validationErrors, int errorCount, string setBy)
        {
            StepSubmissionId = stepSubmissionId;
            FormSubmissionId = formSubmissionId;
            FormStepId = formStepId;
            ValidationErrors = validationErrors;
            ErrorCount = errorCount;
            SetBy = setBy;
        }
    }

    /// <summary>
    /// Event raised when a form step submission is restarted.
    /// </summary>
    public class FormStepSubmissionRestartedEvent : DomainEvent
    {
        public Guid StepSubmissionId { get; }
        public Guid FormSubmissionId { get; }
        public Guid FormStepId { get; }
        public int StepNumber { get; }
        public string TenantId { get; }
        public Guid UserId { get; }
        public string RestartedBy { get; }

        public FormStepSubmissionRestartedEvent(Guid stepSubmissionId, Guid formSubmissionId, Guid formStepId, int stepNumber, string tenantId, Guid userId, string restartedBy)
        {
            StepSubmissionId = stepSubmissionId;
            FormSubmissionId = formSubmissionId;
            FormStepId = formStepId;
            StepNumber = stepNumber;
            TenantId = tenantId;
            UserId = userId;
            RestartedBy = restartedBy;
        }
    }
}