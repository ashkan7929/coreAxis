using CoreAxis.SharedKernel.DomainEvents;
using System;

namespace CoreAxis.Modules.DynamicForm.Domain.Events
{
    /// <summary>
    /// Event raised when a form access policy is created.
    /// </summary>
    public class FormAccessPolicyCreatedEvent : DomainEvent
    {
        public Guid PolicyId { get; }
        public Guid FormId { get; }
        public string PolicyType { get; }
        public string TenantId { get; }

        public FormAccessPolicyCreatedEvent(Guid policyId, Guid formId, string policyType, string tenantId)
        {
            PolicyId = policyId;
            FormId = formId;
            PolicyType = policyType;
            TenantId = tenantId;
        }
    }

    /// <summary>
    /// Event raised when a form access policy is updated.
    /// </summary>
    public class FormAccessPolicyUpdatedEvent : DomainEvent
    {
        public Guid PolicyId { get; }
        public Guid FormId { get; }
        public string PolicyType { get; }
        public string TenantId { get; }

        public FormAccessPolicyUpdatedEvent(Guid policyId, Guid formId, string policyType, string tenantId)
        {
            PolicyId = policyId;
            FormId = formId;
            PolicyType = policyType;
            TenantId = tenantId;
        }
    }

    /// <summary>
    /// Event raised when a form access policy is enabled.
    /// </summary>
    public class FormAccessPolicyEnabledEvent : DomainEvent
    {
        public Guid PolicyId { get; }
        public Guid FormId { get; }
        public string TenantId { get; }

        public FormAccessPolicyEnabledEvent(Guid policyId, Guid formId, string tenantId)
        {
            PolicyId = policyId;
            FormId = formId;
            TenantId = tenantId;
        }
    }

    /// <summary>
    /// Event raised when a form access policy is disabled.
    /// </summary>
    public class FormAccessPolicyDisabledEvent : DomainEvent
    {
        public Guid PolicyId { get; }
        public Guid FormId { get; }
        public string TenantId { get; }

        public FormAccessPolicyDisabledEvent(Guid policyId, Guid formId, string tenantId)
        {
            PolicyId = policyId;
            FormId = formId;
            TenantId = tenantId;
        }
    }
}