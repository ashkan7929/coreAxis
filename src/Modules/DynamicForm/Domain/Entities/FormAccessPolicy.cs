using CoreAxis.Modules.DynamicForm.Domain.Events;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents an access policy for a form defining who can access and what actions they can perform.
    /// </summary>
    public class FormAccessPolicy : EntityBase
    {
        /// <summary>
        /// Gets or sets the form identifier that this policy applies to.
        /// </summary>
        [Required]
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the policy name.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the policy description.
        /// </summary>
        [MaxLength(1000)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the policy type (User, Role, Group, Public, etc.).
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PolicyType { get; set; }

        /// <summary>
        /// Gets or sets the target identifier (UserId, RoleId, GroupId, etc.).
        /// </summary>
        [MaxLength(100)]
        public string TargetId { get; set; }

        /// <summary>
        /// Gets or sets the target name for display purposes.
        /// </summary>
        [MaxLength(200)]
        public string TargetName { get; set; }

        /// <summary>
        /// Gets or sets the permissions as JSON (read, write, submit, approve, etc.).
        /// </summary>
        [Required]
        public string Permissions { get; set; }

        /// <summary>
        /// Gets or sets the conditions for this policy as JSON expression.
        /// </summary>
        public string Conditions { get; set; }

        /// <summary>
        /// Gets or sets the priority of this policy (lower values have higher priority).
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether this policy is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the effective start date for this policy.
        /// </summary>
        public DateTime? EffectiveFrom { get; set; }

        /// <summary>
        /// Gets or sets the effective end date for this policy.
        /// </summary>
        public DateTime? EffectiveTo { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for this policy as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the parent form.
        /// </summary>
        public virtual Form Form { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormAccessPolicy"/> class.
        /// </summary>
        protected FormAccessPolicy()
        {
            // Required for EF Core
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormAccessPolicy"/> class.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="name">The policy name.</param>
        /// <param name="policyType">The policy type.</param>
        /// <param name="permissions">The permissions as JSON.</param>
        /// <param name="createdBy">The user who created this policy.</param>
        /// <param name="targetId">The target identifier.</param>
        /// <param name="targetName">The target name.</param>
        public FormAccessPolicy(Guid formId, string tenantId, string name, string policyType, string permissions, string createdBy, string targetId = null, string targetName = null)
        {
            if (formId == Guid.Empty)
                throw new ArgumentException("Form ID cannot be empty.", nameof(formId));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Policy name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(policyType))
                throw new ArgumentException("Policy type cannot be null or empty.", nameof(policyType));
            if (string.IsNullOrWhiteSpace(permissions))
                throw new ArgumentException("Permissions cannot be null or empty.", nameof(permissions));

            Id = Guid.NewGuid();
            FormId = formId;
            TenantId = tenantId;
            Name = name;
            PolicyType = policyType;
            TargetId = targetId;
            TargetName = targetName;
            Permissions = permissions;
            CreatedBy = createdBy;
            CreatedOn = DateTime.UtcNow;
            IsActive = true;

            AddDomainEvent(new FormAccessPolicyCreatedEvent(Id, FormId, PolicyType, TenantId));
        }

        /// <summary>
        /// Updates the policy with new information.
        /// </summary>
        /// <param name="name">The new name.</param>
        /// <param name="description">The new description.</param>
        /// <param name="permissions">The new permissions.</param>
        /// <param name="modifiedBy">The user who modified the policy.</param>
        /// <param name="conditions">The new conditions.</param>
        /// <param name="priority">The new priority.</param>
        public void Update(string name, string description, string permissions, string modifiedBy, string conditions = null, int? priority = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Policy name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(permissions))
                throw new ArgumentException("Permissions cannot be null or empty.", nameof(permissions));

            Name = name;
            Description = description;
            Permissions = permissions;
            Conditions = conditions;
            if (priority.HasValue)
                Priority = priority.Value;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormAccessPolicyUpdatedEvent(Id, FormId, PolicyType, TenantId));
        }

        /// <summary>
        /// Enables the policy.
        /// </summary>
        /// <param name="enabledBy">The user who enabled the policy.</param>
        public void Enable(string enabledBy)
        {
            if (IsEnabled)
                return;

            IsEnabled = true;
            LastModifiedBy = enabledBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormAccessPolicyEnabledEvent(Id, FormId, TenantId));
        }

        /// <summary>
        /// Disables the policy.
        /// </summary>
        /// <param name="disabledBy">The user who disabled the policy.</param>
        public void Disable(string disabledBy)
        {
            if (!IsEnabled)
                return;

            IsEnabled = false;
            LastModifiedBy = disabledBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormAccessPolicyDisabledEvent(Id, FormId, TenantId));
        }

        /// <summary>
        /// Sets the effective period for this policy.
        /// </summary>
        /// <param name="effectiveFrom">The start date.</param>
        /// <param name="effectiveTo">The end date.</param>
        /// <param name="modifiedBy">The user who set the effective period.</param>
        public void SetEffectivePeriod(DateTime? effectiveFrom, DateTime? effectiveTo, string modifiedBy)
        {
            if (effectiveFrom.HasValue && effectiveTo.HasValue && effectiveFrom > effectiveTo)
                throw new ArgumentException("Effective from date cannot be greater than effective to date.");

            EffectiveFrom = effectiveFrom;
            EffectiveTo = effectiveTo;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the policy is currently effective.
        /// </summary>
        /// <returns>True if the policy is effective, otherwise false.</returns>
        public bool IsEffective()
        {
            if (!IsEnabled)
                return false;

            var now = DateTime.UtcNow;
            
            if (EffectiveFrom.HasValue && now < EffectiveFrom.Value)
                return false;

            if (EffectiveTo.HasValue && now > EffectiveTo.Value)
                return false;

            return true;
        }
    }



    /// <summary>
    /// Static class containing policy types.
    /// </summary>
    public static class PolicyTypes
    {
        public const string User = "User";
        public const string Role = "Role";
        public const string Group = "Group";
        public const string Public = "Public";
        public const string Anonymous = "Anonymous";
        public const string Tenant = "Tenant";
        public const string Custom = "Custom";
    }

    /// <summary>
    /// Static class containing permission types.
    /// </summary>
    public static class PermissionTypes
    {
        public const string Read = "read";
        public const string Write = "write";
        public const string Submit = "submit";
        public const string Approve = "approve";
        public const string Reject = "reject";
        public const string Delete = "delete";
        public const string Export = "export";
        public const string ViewSubmissions = "view_submissions";
        public const string ManageAccess = "manage_access";
        public const string Publish = "publish";
    }
}