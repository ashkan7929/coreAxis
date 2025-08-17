using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.FormStepSubmissions
{
    /// <summary>
    /// Command for completing a form step submission.
    /// </summary>
    public class CompleteFormStepSubmissionCommand : IRequest<FormStepSubmissionDto>
    {
        /// <summary>
        /// Gets or sets the unique identifier of the form step submission to complete.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the final step data as JSON.
        /// </summary>
        public string StepData { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the user who is completing the step submission.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Completed by cannot exceed 100 characters.")]
        public string CompletedBy { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Tenant ID cannot exceed 100 characters.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to validate the step data before completion.
        /// </summary>
        public bool ValidateBeforeCompletion { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically move to the next step after completion.
        /// </summary>
        public bool AutoMoveToNextStep { get; set; } = true;
    }
}