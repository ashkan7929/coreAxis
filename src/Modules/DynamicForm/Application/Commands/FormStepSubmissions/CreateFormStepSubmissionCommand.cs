using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.FormStepSubmissions
{
    /// <summary>
    /// Command for creating a new form step submission.
    /// </summary>
    public class CreateFormStepSubmissionCommand : IRequest<FormStepSubmissionDto>
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
        /// Gets or sets the user identifier who is submitting this step.
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
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the user who is creating the step submission.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Created by cannot exceed 100 characters.")]
        public string CreatedBy { get; set; }
    }
}