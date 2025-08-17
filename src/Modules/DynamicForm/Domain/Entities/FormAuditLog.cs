using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using CoreAxis.Modules.DynamicForm.Domain.Events;
using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents an audit log entry for form-related activities.
    /// </summary>
    public class FormAuditLog : EntityBase
    {
        /// <summary>
        /// Gets or sets the form identifier that this audit log relates to.
        /// </summary>
        public Guid? FormId { get; set; }

        /// <summary>
        /// Gets or sets the form submission identifier that this audit log relates to.
        /// </summary>
        public Guid? FormSubmissionId { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the action that was performed (Create, Update, Delete, Submit, Approve, etc.).
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the entity type that was affected (Form, FormSubmission, FormField, etc.).
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the entity identifier that was affected.
        /// </summary>
        [Required]
        public Guid EntityId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who performed the action.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user name who performed the action.
        /// </summary>
        [MaxLength(200)]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the IP address from which the action was performed.
        /// </summary>
        [MaxLength(45)]
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent of the client that performed the action.
        /// </summary>
        [MaxLength(500)]
        public string UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the action was performed.
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the old values before the change as JSON.
        /// </summary>
        public string OldValues { get; set; }

        /// <summary>
        /// Gets or sets the new values after the change as JSON.
        /// </summary>
        public string NewValues { get; set; }

        /// <summary>
        /// Gets or sets the changes made as JSON (field-level changes).
        /// </summary>
        public string Changes { get; set; }

        /// <summary>
        /// Gets or sets additional details about the action as JSON.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Gets or sets the reason or comment for the action.
        /// </summary>
        [MaxLength(1000)]
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        [MaxLength(100)]
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier for tracking related actions.
        /// </summary>
        [MaxLength(100)]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the severity level of the action (Info, Warning, Error, Critical).
        /// </summary>
        [MaxLength(20)]
        public string Severity { get; set; } = "Info";

        /// <summary>
        /// Gets or sets the category of the action (Security, Data, Configuration, etc.).
        /// </summary>
        [MaxLength(50)]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the parent form.
        /// </summary>
        public virtual Form Form { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the form submission.
        /// </summary>
        public virtual FormSubmission FormSubmission { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormAuditLog"/> class.
        /// </summary>
        protected FormAuditLog()
        {
            // Required for EF Core
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormAuditLog"/> class.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="action">The action performed.</param>
        /// <param name="entityType">The entity type affected.</param>
        /// <param name="entityId">The entity identifier affected.</param>
        /// <param name="userId">The user who performed the action.</param>
        /// <param name="userName">The user name who performed the action.</param>
        /// <param name="formId">The form identifier (optional).</param>
        /// <param name="formSubmissionId">The form submission identifier (optional).</param>
        public FormAuditLog(string tenantId, string action, string entityType, Guid entityId, string userId, string userName = null, Guid? formId = null, Guid? formSubmissionId = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action cannot be null or empty.", nameof(action));
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type cannot be null or empty.", nameof(entityType));
            if (entityId == Guid.Empty)
                throw new ArgumentException("Entity ID cannot be empty.", nameof(entityId));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            Id = Guid.NewGuid();
            TenantId = tenantId;
            Action = action;
            EntityType = entityType;
            EntityId = entityId;
            UserId = userId;
            UserName = userName;
            FormId = formId;
            FormSubmissionId = formSubmissionId;
            Timestamp = DateTime.UtcNow;
            CreatedOn = DateTime.UtcNow;
            IsActive = true;

            AddDomainEvent(new FormAuditLogCreatedEvent(Id, TenantId, Action, EntityType, EntityId, UserId));
        }

        /// <summary>
        /// Sets the old and new values for the audit log.
        /// </summary>
        /// <param name="oldValues">The old values as JSON.</param>
        /// <param name="newValues">The new values as JSON.</param>
        /// <param name="changes">The changes as JSON.</param>
        public void SetValues(string oldValues, string newValues, string changes = null)
        {
            OldValues = oldValues;
            NewValues = newValues;
            Changes = changes;
        }

        /// <summary>
        /// Sets the client information for the audit log.
        /// </summary>
        /// <param name="ipAddress">The IP address.</param>
        /// <param name="userAgent">The user agent.</param>
        /// <param name="sessionId">The session identifier.</param>
        public void SetClientInfo(string ipAddress, string userAgent, string sessionId = null)
        {
            IpAddress = ipAddress;
            UserAgent = userAgent;
            SessionId = sessionId;
        }

        /// <summary>
        /// Sets additional details for the audit log.
        /// </summary>
        /// <param name="details">The details as JSON.</param>
        /// <param name="reason">The reason for the action.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="severity">The severity level.</param>
        /// <param name="category">The category.</param>
        public void SetDetails(string details, string reason = null, string correlationId = null, string severity = null, string category = null)
        {
            Details = details;
            Reason = reason;
            CorrelationId = correlationId;
            if (!string.IsNullOrWhiteSpace(severity))
                Severity = severity;
            Category = category;
        }

        /// <summary>
        /// Sets the metadata for the audit log.
        /// </summary>
        /// <param name="metadata">The metadata as JSON.</param>
        public void SetMetadata(string metadata)
        {
            Metadata = metadata;
        }

        /// <summary>
        /// Creates an audit log for form creation.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="formData">The form data as JSON.</param>
        /// <returns>A new FormAuditLog instance.</returns>
        public static FormAuditLog ForFormCreated(Guid formId, string tenantId, string userId, string userName, string formData)
        {
            var auditLog = new FormAuditLog(tenantId, AuditActions.Create, EntityTypes.Form, formId, userId, userName, formId);
            auditLog.SetValues(null, formData);
            auditLog.SetDetails(null, "Form created", null, SeverityLevels.Info, Categories.Data);
            return auditLog;
        }

        /// <summary>
        /// Creates an audit log for form update.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="oldData">The old form data as JSON.</param>
        /// <param name="newData">The new form data as JSON.</param>
        /// <param name="changes">The changes as JSON.</param>
        /// <returns>A new FormAuditLog instance.</returns>
        public static FormAuditLog ForFormUpdated(Guid formId, string tenantId, string userId, string userName, string oldData, string newData, string changes)
        {
            var auditLog = new FormAuditLog(tenantId, AuditActions.Update, EntityTypes.Form, formId, userId, userName, formId);
            auditLog.SetValues(oldData, newData, changes);
            auditLog.SetDetails(null, "Form updated", null, SeverityLevels.Info, Categories.Data);
            return auditLog;
        }

        /// <summary>
        /// Creates an audit log for form submission.
        /// </summary>
        /// <param name="submissionId">The submission identifier.</param>
        /// <param name="formId">The form identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="submissionData">The submission data as JSON.</param>
        /// <returns>A new FormAuditLog instance.</returns>
        public static FormAuditLog ForFormSubmitted(Guid submissionId, Guid formId, string tenantId, string userId, string userName, string submissionData)
        {
            var auditLog = new FormAuditLog(tenantId, AuditActions.Submit, EntityTypes.FormSubmission, submissionId, userId, userName, formId, submissionId);
            auditLog.SetValues(null, submissionData);
            auditLog.SetDetails(null, "Form submitted", null, SeverityLevels.Info, Categories.Data);
            return auditLog;
        }

        /// <summary>
        /// Creates an audit log for form access.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="accessType">The access type (View, Download, etc.).</param>
        /// <returns>A new FormAuditLog instance.</returns>
        public static FormAuditLog ForFormAccessed(Guid formId, string tenantId, string userId, string userName, string accessType)
        {
            var auditLog = new FormAuditLog(tenantId, AuditActions.Access, EntityTypes.Form, formId, userId, userName, formId);
            auditLog.SetDetails($"{{\"accessType\":\"{accessType}\"}}", $"Form accessed - {accessType}", null, SeverityLevels.Info, Categories.Security);
            return auditLog;
        }
    }



    /// <summary>
    /// Static class containing audit action types.
    /// </summary>
    public static class AuditActions
    {
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string Submit = "Submit";
        public const string Approve = "Approve";
        public const string Reject = "Reject";
        public const string Publish = "Publish";
        public const string Unpublish = "Unpublish";
        public const string Access = "Access";
        public const string Export = "Export";
        public const string Import = "Import";
        public const string Clone = "Clone";
        public const string Archive = "Archive";
        public const string Restore = "Restore";
        public const string Lock = "Lock";
        public const string Unlock = "Unlock";
        public const string Share = "Share";
        public const string Unshare = "Unshare";
    }

    /// <summary>
    /// Static class containing entity types.
    /// </summary>
    public static class EntityTypes
    {
        public const string Form = "Form";
        public const string FormField = "FormField";
        public const string FormSubmission = "FormSubmission";
        public const string FormVersion = "FormVersion";
        public const string FormAccessPolicy = "FormAccessPolicy";
        public const string FormulaDefinition = "FormulaDefinition";
        public const string FormulaEvaluationLog = "FormulaEvaluationLog";
    }

    /// <summary>
    /// Static class containing severity levels.
    /// </summary>
    public static class SeverityLevels
    {
        public const string Info = "Info";
        public const string Warning = "Warning";
        public const string Error = "Error";
        public const string Critical = "Critical";
    }

    /// <summary>
    /// Static class containing audit categories.
    /// </summary>
    public static class Categories
    {
        public const string Security = "Security";
        public const string Data = "Data";
        public const string Configuration = "Configuration";
        public const string Performance = "Performance";
        public const string Integration = "Integration";
        public const string Compliance = "Compliance";
    }
}