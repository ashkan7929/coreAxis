using CoreAxis.Modules.DynamicForm.Domain.Events;
using CoreAxis.SharedKernel;
using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities
{
    /// <summary>
    /// Represents a submission for a specific step in a multi-step form.
    /// </summary>
    public class FormStepSubmission : EntityBase
    {
        /// <summary>
        /// Gets or sets the form submission identifier that this step submission belongs to.
        /// </summary>
        [Required]
        public Guid FormSubmissionId { get; set; }

        /// <summary>
        /// Gets or sets the form step identifier.
        /// </summary>
        [Required]
        public Guid FormStepId { get; set; }

        /// <summary>
        /// Gets or sets the step number for quick reference.
        /// </summary>
        [Required]
        public int StepNumber { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who submitted this step.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the step submission data as JSON.
        /// </summary>
        [Required]
        public string StepData { get; set; }

        /// <summary>
        /// Gets or sets the status of this step submission.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = StepSubmissionStatus.InProgress;

        /// <summary>
        /// Gets or sets the validation errors for this step as JSON (if any).
        /// </summary>
        public string ValidationErrors { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the user started this step.
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the user completed this step.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the time spent on this step in seconds.
        /// </summary>
        public int? TimeSpentSeconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this step was skipped.
        /// </summary>
        public bool IsSkipped { get; set; } = false;

        /// <summary>
        /// Gets or sets the reason for skipping this step (if applicable).
        /// </summary>
        [MaxLength(500)]
        public string SkipReason { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for this step submission as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the parent form submission.
        /// </summary>
        public virtual FormSubmission FormSubmission { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the form step.
        /// </summary>
        public virtual FormStep FormStep { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStepSubmission"/> class.
        /// </summary>
        protected FormStepSubmission()
        {
            // Required for EF Core
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStepSubmission"/> class.
        /// </summary>
        /// <param name="formSubmissionId">The form submission identifier.</param>
        /// <param name="formStepId">The form step identifier.</param>
        /// <param name="stepNumber">The step number.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="stepData">The step data as JSON.</param>
        public FormStepSubmission(Guid formSubmissionId, Guid formStepId, int stepNumber, Guid userId, string tenantId, string stepData)
        {
            if (formSubmissionId == Guid.Empty)
                throw new ArgumentException("Form submission ID cannot be empty.", nameof(formSubmissionId));
            if (formStepId == Guid.Empty)
                throw new ArgumentException("Form step ID cannot be empty.", nameof(formStepId));
            if (stepNumber <= 0)
                throw new ArgumentException("Step number must be positive.", nameof(stepNumber));
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(stepData))
                throw new ArgumentException("Step data cannot be null or empty.", nameof(stepData));

            Id = Guid.NewGuid();
            FormSubmissionId = formSubmissionId;
            FormStepId = formStepId;
            StepNumber = stepNumber;
            UserId = userId;
            TenantId = tenantId;
            StepData = stepData;
            Status = StepSubmissionStatus.InProgress;
            StartedAt = DateTime.UtcNow;
            CreatedBy = userId.ToString();
            CreatedOn = DateTime.UtcNow;
            IsActive = true;

            AddDomainEvent(new FormStepSubmissionStartedEvent(Id, FormSubmissionId, FormStepId, StepNumber, TenantId, UserId, CreatedBy));
        }

        /// <summary>
        /// Updates the step data.
        /// </summary>
        /// <param name="stepData">The new step data as JSON.</param>
        /// <param name="modifiedBy">The user who modified the step data.</param>
        public void UpdateStepData(string stepData, string modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(stepData))
                throw new ArgumentException("Step data cannot be null or empty.", nameof(stepData));

            if (Status == StepSubmissionStatus.Completed)
                throw new InvalidOperationException("Cannot update data of a completed step.");

            StepData = stepData;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormStepSubmissionUpdatedEvent(Id, FormSubmissionId, FormStepId, StepNumber, stepData, modifiedBy));
        }

        /// <summary>
        /// Completes the step submission.
        /// </summary>
        /// <param name="completedBy">The user who completed the step.</param>
        public void Complete(string completedBy)
        {
            if (Status == StepSubmissionStatus.Completed)
                throw new InvalidOperationException("Step is already completed.");

            if (IsSkipped)
                throw new InvalidOperationException("Cannot complete a skipped step.");

            Status = StepSubmissionStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            
            if (StartedAt.HasValue)
            {
                TimeSpentSeconds = (int)(CompletedAt.Value - StartedAt.Value).TotalSeconds;
            }

            LastModifiedBy = completedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormStepSubmissionCompletedEvent(Id, FormSubmissionId, FormStepId, StepNumber, TenantId, UserId, CompletedAt.Value, TimeSpentSeconds ?? 0, completedBy));
        }

        /// <summary>
        /// Skips the step submission.
        /// </summary>
        /// <param name="reason">The reason for skipping.</param>
        /// <param name="skippedBy">The user who skipped the step.</param>
        public void Skip(string reason, string skippedBy)
        {
            if (Status == StepSubmissionStatus.Completed)
                throw new InvalidOperationException("Cannot skip a completed step.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Skip reason cannot be null or empty.", nameof(reason));

            Status = StepSubmissionStatus.Skipped;
            IsSkipped = true;
            SkipReason = reason;
            CompletedAt = DateTime.UtcNow;
            
            if (StartedAt.HasValue)
            {
                TimeSpentSeconds = (int)(CompletedAt.Value - StartedAt.Value).TotalSeconds;
            }

            LastModifiedBy = skippedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormStepSubmissionSkippedEvent(Id, FormSubmissionId, FormStepId, StepNumber, TenantId, UserId, reason, skippedBy));
        }

        /// <summary>
        /// Sets validation errors for the step submission.
        /// </summary>
        /// <param name="validationErrors">The validation errors as JSON.</param>
        /// <param name="modifiedBy">The user who set the validation errors.</param>
        public void SetValidationErrors(string validationErrors, string modifiedBy)
        {
            ValidationErrors = validationErrors;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;

            // Count validation errors (assuming JSON array format)
            var errorCount = string.IsNullOrWhiteSpace(validationErrors) ? 0 : 
                validationErrors.Split('[', ']', '{', '}').Length - 1;

            AddDomainEvent(new FormStepSubmissionValidationErrorsSetEvent(Id, FormSubmissionId, FormStepId, validationErrors, errorCount, modifiedBy));
        }

        /// <summary>
        /// Clears validation errors for the step submission.
        /// </summary>
        /// <param name="modifiedBy">The user who cleared the validation errors.</param>
        public void ClearValidationErrors(string modifiedBy)
        {
            ValidationErrors = null;
            LastModifiedBy = modifiedBy;
            LastModifiedOn = DateTime.UtcNow;
        }

        /// <summary>
        /// Restarts the step submission.
        /// </summary>
        /// <param name="restartedBy">The user who restarted the step.</param>
        public void Restart(string restartedBy)
        {
            Status = StepSubmissionStatus.InProgress;
            IsSkipped = false;
            SkipReason = null;
            ValidationErrors = null;
            StartedAt = DateTime.UtcNow;
            CompletedAt = null;
            TimeSpentSeconds = null;
            LastModifiedBy = restartedBy;
            LastModifiedOn = DateTime.UtcNow;

            AddDomainEvent(new FormStepSubmissionRestartedEvent(Id, FormSubmissionId, FormStepId, StepNumber, TenantId, UserId, restartedBy));
        }
    }


}