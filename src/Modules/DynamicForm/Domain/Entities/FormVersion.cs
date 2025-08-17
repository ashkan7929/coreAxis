using CoreAxis.Modules.DynamicForm.Domain.Events;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents a version of a form with its schema and metadata.
    /// </summary>
    public class FormVersion : EntityBase
    {
        /// <summary>
        /// Gets or sets the form identifier that this version belongs to.
        /// </summary>
        [Required]
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the version number.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the JSON schema definition of this version.
        /// </summary>
        [Required]
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this version is published.
        /// </summary>
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this is the current active version.
        /// </summary>
        public bool IsCurrent { get; set; } = false;

        /// <summary>
        /// Gets or sets the timestamp when this version was published.
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who published this version.
        /// </summary>
        [MaxLength(100)]
        public string PublishedBy { get; set; }

        /// <summary>
        /// Gets or sets the changelog or description of changes in this version.
        /// </summary>
        [MaxLength(2000)]
        public string ChangeLog { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for this version as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the parent form.
        /// </summary>
        public virtual Form Form { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormVersion"/> class.
        /// </summary>
        protected FormVersion()
        {
            // Required for EF Core
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormVersion"/> class.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="version">The version number.</param>
        /// <param name="schema">The JSON schema definition.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="createdBy">The user who created this version.</param>
        /// <param name="changeLog">The changelog for this version.</param>
        public FormVersion(Guid formId, int version, string schema, string tenantId, string createdBy, string changeLog = null)
        {
            if (formId == Guid.Empty)
                throw new ArgumentException("Form ID cannot be empty.", nameof(formId));
            if (version <= 0)
                throw new ArgumentException("Version must be greater than zero.", nameof(version));
            if (string.IsNullOrWhiteSpace(schema))
                throw new ArgumentException("Schema cannot be null or empty.", nameof(schema));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

            Id = Guid.NewGuid();
            FormId = formId;
            Version = version;
            Schema = schema;
            TenantId = tenantId;
            ChangeLog = changeLog;
            CreatedBy = createdBy;
            CreatedOn = DateTime.UtcNow;
            IsActive = true;

            AddDomainEvent(new FormVersionCreatedEvent(Id, FormId, Version, TenantId));
        }

        /// <summary>
        /// Publishes this version making it available for use.
        /// </summary>
        /// <param name="publishedBy">The user who published this version.</param>
        public void Publish(string publishedBy)
        {
            if (IsPublished)
                throw new InvalidOperationException("Version is already published.");

            IsPublished = true;
            PublishedAt = DateTime.UtcNow;
            PublishedBy = publishedBy;
            LastModifiedBy = publishedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormVersionPublishedEvent(Id, FormId, Version, TenantId));
        }

        /// <summary>
        /// Sets this version as the current active version.
        /// </summary>
        /// <param name="activatedBy">The user who activated this version.</param>
        public void SetAsCurrent(string activatedBy)
        {
            if (!IsPublished)
                throw new InvalidOperationException("Only published versions can be set as current.");

            IsCurrent = true;
            LastModifiedBy = activatedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormVersionActivatedEvent(Id, FormId, Version, TenantId));
        }

        /// <summary>
        /// Removes the current status from this version.
        /// </summary>
        /// <param name="deactivatedBy">The user who deactivated this version.</param>
        public void RemoveFromCurrent(string deactivatedBy)
        {
            IsCurrent = false;
            LastModifiedBy = deactivatedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormVersionDeactivatedEvent(Id, FormId, Version, TenantId));
        }

        /// <summary>
        /// Updates the changelog for this version.
        /// </summary>
        /// <param name="changeLog">The new changelog.</param>
        /// <param name="modifiedBy">The user who modified the changelog.</param>
        public void UpdateChangeLog(string changeLog, string modifiedBy)
        {
            ChangeLog = changeLog;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;
        }
    }


}