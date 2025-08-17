using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.FormSteps
{
    /// <summary>
    /// Command for creating a new form step.
    /// </summary>
    public class CreateFormStepCommand : IRequest<FormStepDto>
    {
        /// <summary>
        /// Gets or sets the form identifier that this step belongs to.
        /// </summary>
        [Required]
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the step number.
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
        /// Gets or sets the step schema as JSON.
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
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Tenant ID cannot exceed 100 characters.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the user who is creating the step.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Created by cannot exceed 100 characters.")]
        public string CreatedBy { get; set; }
    }
}