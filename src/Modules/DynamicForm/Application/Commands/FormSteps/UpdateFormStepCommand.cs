using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.FormSteps
{
    /// <summary>
    /// Command for updating an existing form step.
    /// </summary>
    public class UpdateFormStepCommand : IRequest<FormStepDto>
    {
        /// <summary>
        /// Gets or sets the unique identifier of the form step to update.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the title of the step.
        /// </summary>
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the step.
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the step schema as JSON.
        /// </summary>
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
        public bool? IsRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this step can be skipped.
        /// </summary>
        public bool? CanSkip { get; set; }

        /// <summary>
        /// Gets or sets the minimum time required to spend on this step in seconds.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Minimum time must be non-negative.")]
        public int? MinTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum time allowed for this step in seconds.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Maximum time must be non-negative.")]
        public int? MaxTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the step is active.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Gets or sets the user who is updating the step.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Last modified by cannot exceed 100 characters.")]
        public string LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Tenant ID cannot exceed 100 characters.")]
        public string TenantId { get; set; }
    }
}