using System;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.DynamicForm.Application.DTOs
{
    /// <summary>
    /// Data transfer object for form step information.
    /// </summary>
    public class FormStepDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the form step.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the form identifier that this step belongs to.
        /// </summary>
        [Required]
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the step number (order) within the form.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Step number must be greater than 0.")]
        public int StepNumber { get; set; }

        /// <summary>
        /// Gets or sets the title of the step.
        /// </summary>
        [Required]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the step.
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the JSON schema definition for this step.
        /// </summary>
        [Required]
        public string StepSchema { get; set; }

        /// <summary>
        /// Gets or sets the validation rules as JSON.
        /// </summary>
        public string ValidationRules { get; set; }

        /// <summary>
        /// Gets or sets the conditional logic as JSON.
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
        /// Gets or sets the minimum time in seconds required to complete this step.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Minimum time must be non-negative.")]
        public int? MinTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum time in seconds allowed to complete this step.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Maximum time must be greater than 0.")]
        public int? MaxTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Tenant ID cannot exceed 100 characters.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the user who created the step.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Created by cannot exceed 100 characters.")]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the last modification timestamp.
        /// </summary>
        public DateTime? LastModifiedOn { get; set; }

        /// <summary>
        /// Gets or sets the user who last modified the step.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Last modified by cannot exceed 100 characters.")]
        public string LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the step is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Data transfer object for creating a new form step.
    /// </summary>
    public class CreateFormStepDto
    {
        /// <summary>
        /// Gets or sets the form identifier that this step belongs to.
        /// </summary>
        [Required]
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the step number (order) within the form.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Step number must be greater than 0.")]
        public int StepNumber { get; set; }

        /// <summary>
        /// Gets or sets the title of the step.
        /// </summary>
        [Required]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the step.
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the JSON schema definition for this step.
        /// </summary>
        [Required]
        public string StepSchema { get; set; }

        /// <summary>
        /// Gets or sets the validation rules as JSON.
        /// </summary>
        public string ValidationRules { get; set; }

        /// <summary>
        /// Gets or sets the conditional logic as JSON.
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
        /// Gets or sets the minimum time in seconds required to complete this step.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Minimum time must be non-negative.")]
        public int? MinTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum time in seconds allowed to complete this step.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Maximum time must be greater than 0.")]
        public int? MaxTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }
    }

    /// <summary>
    /// Data transfer object for updating an existing form step.
    /// </summary>
    public class UpdateFormStepDto
    {
        /// <summary>
        /// Gets or sets the title of the step.
        /// </summary>
        [Required]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the step.
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the JSON schema definition for this step.
        /// </summary>
        [Required]
        public string StepSchema { get; set; }

        /// <summary>
        /// Gets or sets the validation rules as JSON.
        /// </summary>
        public string ValidationRules { get; set; }

        /// <summary>
        /// Gets or sets the conditional logic as JSON.
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
        /// Gets or sets the minimum time in seconds required to complete this step.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Minimum time must be non-negative.")]
        public int? MinTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum time in seconds allowed to complete this step.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Maximum time must be greater than 0.")]
        public int? MaxTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }
    }
}