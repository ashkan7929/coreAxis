using CoreAxis.Modules.DynamicForm.Domain.Events;
using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents a step in a multi-step form.
    /// </summary>
    public class FormStep : EntityBase
    {
        /// <summary>
        /// Gets or sets the form identifier that this step belongs to.
        /// </summary>
        [Required]
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the step number (order) in the form.
        /// </summary>
        [Required]
        public int StepNumber { get; set; }

        /// <summary>
        /// Gets or sets the title of the step.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the step.
        /// </summary>
        [MaxLength(1000)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the JSON schema for fields in this step.
        /// </summary>
        [Required]
        public string StepSchema { get; set; }

        /// <summary>
        /// Gets or sets the validation rules for this step as JSON.
        /// </summary>
        public string ValidationRules { get; set; }

        /// <summary>
        /// Gets or sets the conditional logic for showing this step as JSON.
        /// </summary>
        public string ConditionalLogic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this step is required.
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this step can be skipped.
        /// </summary>
        public bool CanSkip { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this step can be repeated.
        /// </summary>
        public bool IsRepeatable { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this step is skippable.
        /// </summary>
        public bool IsSkippable { get; set; } = false;

        /// <summary>
        /// Gets or sets the type of the step.
        /// </summary>
        [MaxLength(50)]
        public string StepType { get; set; } = "Standard";

        /// <summary>
        /// Gets or sets the minimum time (in seconds) user should spend on this step.
        /// </summary>
        public int? MinTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum time (in seconds) user can spend on this step.
        /// </summary>
        public int? MaxTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the step as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the list of step numbers that this step depends on.
        /// </summary>
        public string DependsOnSteps { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the parent form.
        /// </summary>
        public virtual Form Form { get; set; }

        /// <summary>
        /// Gets or sets the collection of submissions for this step.
        /// </summary>
        public virtual ICollection<FormStepSubmission> Submissions { get; set; } = new List<FormStepSubmission>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStep"/> class.
        /// </summary>
        protected FormStep()
        {
            // Required for EF Core
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStep"/> class.
        /// </summary>
        /// <param name="formId">The form identifier.</param>
        /// <param name="stepNumber">The step number.</param>
        /// <param name="title">The step title.</param>
        /// <param name="stepSchema">The step schema as JSON.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="createdBy">The user who created the step.</param>
        public FormStep(Guid formId, int stepNumber, string title, string stepSchema, string tenantId, string createdBy)
        {
            if (formId == Guid.Empty)
                throw new ArgumentException("Form ID cannot be empty.", nameof(formId));
            if (stepNumber <= 0)
                throw new ArgumentException("Step number must be positive.", nameof(stepNumber));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Step title cannot be null or empty.", nameof(title));
            if (string.IsNullOrWhiteSpace(stepSchema))
                throw new ArgumentException("Step schema cannot be null or empty.", nameof(stepSchema));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

            Id = Guid.NewGuid();
            FormId = formId;
            StepNumber = stepNumber;
            Title = title;
            StepSchema = stepSchema;
            TenantId = tenantId;
            CreatedBy = createdBy;
            CreatedOn = DateTime.UtcNow;
            IsActive = true;

            AddDomainEvent(new FormStepCreatedEvent(Id, FormId, StepNumber, Title, createdBy));
        }

        /// <summary>
        /// Updates the step information.
        /// </summary>
        /// <param name="title">The new title.</param>
        /// <param name="description">The new description.</param>
        /// <param name="stepSchema">The new step schema.</param>
        /// <param name="modifiedBy">The user who modified the step.</param>
        public void Update(string title, string description, string stepSchema, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Step title cannot be null or empty.", nameof(title));
            if (string.IsNullOrWhiteSpace(stepSchema))
                throw new ArgumentException("Step schema cannot be null or empty.", nameof(stepSchema));

            Title = title;
            Description = description;
            StepSchema = stepSchema;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormStepUpdatedEvent(Id, FormId, StepNumber, Title, modifiedBy));
        }

        /// <summary>
        /// Sets the validation rules for this step.
        /// </summary>
        /// <param name="validationRules">The validation rules as JSON.</param>
        /// <param name="modifiedBy">The user who set the validation rules.</param>
        public void SetValidationRules(string validationRules, string modifiedBy)
        {
            ValidationRules = validationRules;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the conditional logic for this step.
        /// </summary>
        /// <param name="conditionalLogic">The conditional logic as JSON.</param>
        /// <param name="modifiedBy">The user who set the conditional logic.</param>
        public void SetConditionalLogic(string conditionalLogic, string modifiedBy)
        {
            ConditionalLogic = conditionalLogic;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the time constraints for this step.
        /// </summary>
        /// <param name="minTimeSeconds">The minimum time in seconds.</param>
        /// <param name="maxTimeSeconds">The maximum time in seconds.</param>
        /// <param name="modifiedBy">The user who set the time constraints.</param>
        public void SetTimeConstraints(int? minTimeSeconds, int? maxTimeSeconds, string modifiedBy)
        {
            if (minTimeSeconds.HasValue && minTimeSeconds.Value < 0)
                throw new ArgumentException("Minimum time cannot be negative.", nameof(minTimeSeconds));
            if (maxTimeSeconds.HasValue && maxTimeSeconds.Value < 0)
                throw new ArgumentException("Maximum time cannot be negative.", nameof(maxTimeSeconds));
            if (minTimeSeconds.HasValue && maxTimeSeconds.HasValue && minTimeSeconds.Value > maxTimeSeconds.Value)
                throw new ArgumentException("Minimum time cannot be greater than maximum time.");

            MinTimeSeconds = minTimeSeconds;
            MaxTimeSeconds = maxTimeSeconds;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Reorders the step to a new position.
        /// </summary>
        /// <param name="newStepNumber">The new step number.</param>
        /// <param name="modifiedBy">The user who reordered the step.</param>
        public void Reorder(int newStepNumber, string modifiedBy)
        {
            if (newStepNumber <= 0)
                throw new ArgumentException("Step number must be positive.", nameof(newStepNumber));

            var oldStepNumber = StepNumber;
            StepNumber = newStepNumber;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormStepReorderedEvent(Id, FormId, oldStepNumber, newStepNumber, modifiedBy));
        }
    }
}