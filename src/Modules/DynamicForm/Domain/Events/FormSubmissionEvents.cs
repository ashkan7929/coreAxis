using CoreAxis.SharedKernel.DomainEvents;
using System;

namespace CoreAxis.Modules.DynamicForm.Domain.Events
{
    /// <summary>
    /// Event raised when a new form submission is created.
    /// </summary>
    public class FormSubmissionCreatedEvent : DomainEvent
    {
        public Guid SubmissionId { get; }
        public Guid FormId { get; }
        public string TenantId { get; }
        public Guid? UserId { get; }
        public string Status { get; }
        public string CreatedBy { get; }

        public FormSubmissionCreatedEvent(Guid submissionId, Guid formId, string tenantId, Guid? userId, string status, string createdBy)
        {
            SubmissionId = submissionId;
            FormId = formId;
            TenantId = tenantId;
            UserId = userId;
            Status = status;
            CreatedBy = createdBy;
        }
    }

    /// <summary>
    /// Event raised when a form submission is updated.
    /// </summary>
    public class FormSubmissionUpdatedEvent : DomainEvent
    {
        public Guid SubmissionId { get; }
        public Guid FormId { get; }
        public string TenantId { get; }
        public Guid? UserId { get; }
        public string Data { get; }
        public string UpdatedBy { get; }

        public FormSubmissionUpdatedEvent(Guid submissionId, Guid formId, string tenantId, Guid? userId, string data, string updatedBy)
        {
            SubmissionId = submissionId;
            FormId = formId;
            TenantId = tenantId;
            UserId = userId;
            Data = data;
            UpdatedBy = updatedBy;
        }
    }

    /// <summary>
    /// Event raised when a form submission is submitted.
    /// </summary>
    public class FormSubmissionSubmittedEvent : DomainEvent
    {
        public Guid SubmissionId { get; }
        public Guid FormId { get; }
        public string TenantId { get; }
        public Guid? UserId { get; }
        public DateTime SubmittedAt { get; }
        public string IpAddress { get; }
        public string UserAgent { get; }
        public string SubmittedBy { get; }

        public FormSubmissionSubmittedEvent(Guid submissionId, Guid formId, string tenantId, Guid? userId, DateTime submittedAt, string ipAddress, string userAgent, string submittedBy)
        {
            SubmissionId = submissionId;
            FormId = formId;
            TenantId = tenantId;
            UserId = userId;
            SubmittedAt = submittedAt;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            SubmittedBy = submittedBy;
        }
    }

    /// <summary>
    /// Event raised when a form submission is approved.
    /// </summary>
    public class FormSubmissionApprovedEvent : DomainEvent
    {
        public Guid SubmissionId { get; }
        public Guid FormId { get; }
        public string TenantId { get; }
        public Guid? UserId { get; }
        public DateTime ApprovedAt { get; }
        public Guid ApprovedBy { get; }
        public string ApprovedByName { get; }

        public FormSubmissionApprovedEvent(Guid submissionId, Guid formId, string tenantId, Guid? userId, DateTime approvedAt, Guid approvedBy, string approvedByName)
        {
            SubmissionId = submissionId;
            FormId = formId;
            TenantId = tenantId;
            UserId = userId;
            ApprovedAt = approvedAt;
            ApprovedBy = approvedBy;
            ApprovedByName = approvedByName;
        }
    }

    /// <summary>
    /// Event raised when a form submission is rejected.
    /// </summary>
    public class FormSubmissionRejectedEvent : DomainEvent
    {
        public Guid SubmissionId { get; }
        public Guid FormId { get; }
        public string TenantId { get; }
        public Guid? UserId { get; }
        public DateTime RejectedAt { get; }
        public Guid RejectedBy { get; }
        public string RejectedByName { get; }
        public string RejectionReason { get; }

        public FormSubmissionRejectedEvent(Guid submissionId, Guid formId, string tenantId, Guid? userId, DateTime rejectedAt, Guid rejectedBy, string rejectedByName, string rejectionReason)
        {
            SubmissionId = submissionId;
            FormId = formId;
            TenantId = tenantId;
            UserId = userId;
            RejectedAt = rejectedAt;
            RejectedBy = rejectedBy;
            RejectedByName = rejectedByName;
            RejectionReason = rejectionReason;
        }
    }

    /// <summary>
    /// Event raised when validation errors are set on a form submission.
    /// </summary>
    public class FormSubmissionValidationErrorsSetEvent : DomainEvent
    {
        public Guid SubmissionId { get; }
        public Guid FormId { get; }
        public string TenantId { get; }
        public string ValidationErrors { get; }
        public int ErrorCount { get; }
        public string SetBy { get; }

        public FormSubmissionValidationErrorsSetEvent(Guid submissionId, Guid formId, string tenantId, string validationErrors, int errorCount, string setBy)
        {
            SubmissionId = submissionId;
            FormId = formId;
            TenantId = tenantId;
            ValidationErrors = validationErrors;
            ErrorCount = errorCount;
            SetBy = setBy;
        }
    }

    /// <summary>
    /// Event raised when validation errors are cleared from a form submission.
    /// </summary>
    public class FormSubmissionValidationErrorsClearedEvent : DomainEvent
    {
        public Guid SubmissionId { get; }
        public Guid FormId { get; }
        public string TenantId { get; }
        public string ClearedBy { get; }

        public FormSubmissionValidationErrorsClearedEvent(Guid submissionId, Guid formId, string tenantId, string clearedBy)
        {
            SubmissionId = submissionId;
            FormId = formId;
            TenantId = tenantId;
            ClearedBy = clearedBy;
        }
    }
}