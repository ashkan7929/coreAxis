using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Application.DTOs
{
    /// <summary>
    /// Data transfer object for form step submission information.
    /// </summary>
    public class FormStepSubmissionDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the form step submission.
        /// </summary>
        public Guid Id { get; set; }

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
        /// Gets or sets the step number.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Step number must be greater than 0.")]
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
        [MaxLength(100, ErrorMessage = "Tenant ID cannot exceed 100 characters.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the step data as JSON.
        /// </summary>
        public string StepData { get; set; }

        /// <summary>
        /// Gets or sets the status of the step submission.
        /// </summary>
        [Required]
        [MaxLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the validation errors as JSON (if any).
        /// </summary>
        public string ValidationErrors { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the step was started.
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the step was completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the time spent on this step in seconds.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Time spent must be non-negative.")]
        public int? TimeSpentSeconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this step was skipped.
        /// </summary>
        public bool IsSkipped { get; set; } = false;

        /// <summary>
        /// Gets or sets the reason for skipping this step.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Skip reason cannot exceed 500 characters.")]
        public string SkipReason { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the user who created the step submission.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Created by cannot exceed 100 characters.")]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the last modification timestamp.
        /// </summary>
        public DateTime? LastModifiedOn { get; set; }

        /// <summary>
        /// Gets or sets the user who last modified the step submission.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Last modified by cannot exceed 100 characters.")]
        public string LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the step submission is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Data transfer object for creating a new form step submission.
    /// </summary>
    public class CreateFormStepSubmissionDto
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
        /// Gets or sets the step number.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Step number must be greater than 0.")]
        public int StepNumber { get; set; }

        /// <summary>
        /// Gets or sets the step data as JSON.
        /// </summary>
        public string StepData { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }
    }

    /// <summary>
    /// Data transfer object for updating an existing form step submission.
    /// </summary>
    public class UpdateFormStepSubmissionDto
    {
        /// <summary>
        /// Gets or sets the step data as JSON.
        /// </summary>
        public string StepData { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }
    }

    /// <summary>
    /// Data transfer object for completing a form step submission.
    /// </summary>
    public class CompleteFormStepSubmissionDto
    {
        /// <summary>
        /// Gets or sets the final step data as JSON.
        /// </summary>
        public string StepData { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }
    }

    /// <summary>
    /// Data transfer object for skipping a form step submission.
    /// </summary>
    public class SkipFormStepSubmissionDto
    {
        /// <summary>
        /// Gets or sets the reason for skipping this step.
        /// </summary>
        [Required]
        [MaxLength(500, ErrorMessage = "Skip reason cannot exceed 500 characters.")]
        public string SkipReason { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }
    }

}