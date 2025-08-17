using CoreAxis.SharedKernel.DomainEvents;
using System;

namespace CoreAxis.Modules.DynamicForm.Domain.Events
{
    /// <summary>
    /// Event raised when a form audit log is created.
    /// </summary>
    public class FormAuditLogCreatedEvent : DomainEvent
    {
        public Guid AuditLogId { get; }
        public string TenantId { get; }
        public string Action { get; }
        public string EntityType { get; }
        public Guid EntityId { get; }
        public string UserId { get; }

        public FormAuditLogCreatedEvent(Guid auditLogId, string tenantId, string action, string entityType, Guid entityId, string userId)
        {
            AuditLogId = auditLogId;
            TenantId = tenantId;
            Action = action;
            EntityType = entityType;
            EntityId = entityId;
            UserId = userId;
        }
    }
}