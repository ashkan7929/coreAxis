using CoreAxis.Modules.DynamicForm.Domain.Events;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.DomainEvents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents a dynamic form definition in the system.
    /// </summary>
    public class Form : EntityBase
    {
        private readonly List<FormField> _fields = new List<FormField>();
        private readonly List<FormSubmission> _submissions = new List<FormSubmission>();
        private readonly List<FormStep> _steps = new List<FormStep>();

        /// <summary>
        /// Gets or sets the name of the form.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the form.
        /// </summary>
        [MaxLength(1000)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the JSON schema definition of the form.
        /// </summary>
        [Required]
        public string Schema { get; set; }

        /// <summary>
        /// Gets or sets the business identifier.
        /// </summary>
        public string BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the version of the form.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether the form is published and available for use.
        /// </summary>
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the form as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets the collection of form fields.
        /// </summary>
        public virtual IReadOnlyCollection<FormField> Fields => _fields.AsReadOnly();

        /// <summary>
        /// Gets the collection of form submissions.
        /// </summary>
        public virtual IReadOnlyCollection<FormSubmission> Submissions => _submissions.AsReadOnly();

        /// <summary>
        /// Gets the collection of form steps for multi-step forms.
        /// </summary>
        public virtual IReadOnlyCollection<FormStep> Steps => _steps.AsReadOnly();

        /// <summary>
        /// Gets or sets the collection of form versions.
        /// </summary>
        public virtual ICollection<FormVersion> Versions { get; set; } = new List<FormVersion>();

        /// <summary>
        /// Gets or sets a value indicating whether this form is a multi-step form.
        /// </summary>
        public bool IsMultiStep { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Form"/> class.
        /// </summary>
        public Form()
        {
            // Required for EF Core and public instantiation
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Form"/> class.
        /// </summary>
        /// <param name="name">The name of the form.</param>
        /// <param name="schema">The JSON schema definition.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="createdBy">The user who created the form.</param>
        public Form(string name, string schema, string tenantId, string createdBy)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Form name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(schema))
                throw new ArgumentException("Form schema cannot be null or empty.", nameof(schema));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

            Id = Guid.NewGuid();
            Name = name;
            Schema = schema;
            TenantId = tenantId;
            CreatedBy = createdBy;
            CreatedOn = DateTime.UtcNow;
            IsActive = true;

            AddDomainEvent(new FormCreatedEvent(Id, Name, TenantId, createdBy));
        }

        /// <summary>
        /// Updates the form with new information.
        /// </summary>
        /// <param name="name">The new name.</param>
        /// <param name="description">The new description.</param>
        /// <param name="schema">The new schema.</param>
        /// <param name="modifiedBy">The user who modified the form.</param>
        public void Update(string name, string description, string schema, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Form name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(schema))
                throw new ArgumentException("Form schema cannot be null or empty.", nameof(schema));

            Name = name;
            Description = description;
            Schema = schema;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormUpdatedEvent(Id, Name, TenantId, modifiedBy));
        }

        /// <summary>
        /// Publishes the form making it available for submissions.
        /// </summary>
        /// <param name="publishedBy">The user who published the form.</param>
        public void Publish(string publishedBy)
        {
            if (IsPublished)
                throw new InvalidOperationException("Form is already published.");

            IsPublished = true;
            LastModifiedBy = publishedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormPublishedEvent(Id, Name, TenantId, Version, publishedBy));
        }

        /// <summary>
        /// Unpublishes the form making it unavailable for new submissions.
        /// </summary>
        /// <param name="unpublishedBy">The user who unpublished the form.</param>
        public void Unpublish(string unpublishedBy)
        {
            if (!IsPublished)
                throw new InvalidOperationException("Form is not published.");

            IsPublished = false;
            LastModifiedBy = unpublishedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormUnpublishedEvent(Id, Name, TenantId, unpublishedBy));
        }

        /// <summary>
        /// Creates a new version of the form.
        /// </summary>
        /// <param name="newSchema">The new schema for the version.</param>
        /// <param name="versionedBy">The user who created the version.</param>
        public void CreateVersion(string newSchema, string versionedBy)
        {
            if (string.IsNullOrWhiteSpace(newSchema))
                throw new ArgumentException("New schema cannot be null or empty.", nameof(newSchema));

            Version++;
            Schema = newSchema;
            IsPublished = false; // New version needs to be published again
            LastModifiedBy = versionedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormVersionCreatedEvent(Guid.NewGuid(), Id, Version, TenantId));
        }

        /// <summary>
        /// Adds a field to the form.
        /// </summary>
        /// <param name="field">The field to add.</param>
        internal void AddField(FormField field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            _fields.Add(field);
        }

        /// <summary>
        /// Adds a submission to the form.
        /// </summary>
        /// <param name="submission">The submission to add.</param>
        internal void AddSubmission(FormSubmission submission)
        {
            if (submission == null)
                throw new ArgumentNullException(nameof(submission));

            if (!IsPublished)
                throw new InvalidOperationException("Cannot submit to an unpublished form.");

            _submissions.Add(submission);
        }

        /// <summary>
        /// Adds a step to the form.
        /// </summary>
        /// <param name="step">The step to add.</param>
        /// <param name="addedBy">The user who added the step.</param>
        public void AddStep(FormStep step, string addedBy)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            if (_steps.Any(s => s.StepNumber == step.StepNumber))
                throw new InvalidOperationException($"A step with number {step.StepNumber} already exists.");

            _steps.Add(step);
            IsMultiStep = _steps.Count > 1;
            LastModifiedBy = addedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormStepCreatedEvent(step.Id, Id, step.StepNumber, step.Title, addedBy));
        }

        /// <summary>
        /// Removes a step from the form.
        /// </summary>
        /// <param name="stepNumber">The step number to remove.</param>
        /// <param name="removedBy">The user who removed the step.</param>
        public void RemoveStep(int stepNumber, string removedBy)
        {
            var step = _steps.FirstOrDefault(s => s.StepNumber == stepNumber);
            if (step == null)
                throw new InvalidOperationException($"Step with number {stepNumber} not found.");

            _steps.Remove(step);
            IsMultiStep = _steps.Count > 1;
            LastModifiedBy = removedBy;
            LastModifiedOn = DateTime.UtcNow;

            // Reorder remaining steps
            var stepsToReorder = _steps.Where(s => s.StepNumber > stepNumber).OrderBy(s => s.StepNumber).ToList();
            foreach (var stepToReorder in stepsToReorder)
            {
                stepToReorder.Reorder(stepToReorder.StepNumber - 1, removedBy);
            }
        }

        /// <summary>
        /// Gets a step by its number.
        /// </summary>
        /// <param name="stepNumber">The step number.</param>
        /// <returns>The form step if found, otherwise null.</returns>
        public FormStep GetStep(int stepNumber)
        {
            return _steps.FirstOrDefault(s => s.StepNumber == stepNumber);
        }

        /// <summary>
        /// Gets all steps ordered by step number.
        /// </summary>
        /// <returns>The ordered collection of form steps.</returns>
        public IEnumerable<FormStep> GetOrderedSteps()
        {
            return _steps.OrderBy(s => s.StepNumber);
        }
    }


}