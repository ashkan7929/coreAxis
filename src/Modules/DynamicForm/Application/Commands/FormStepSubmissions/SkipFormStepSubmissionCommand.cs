using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.FormStepSubmissions
{
    /// <summary>
    /// Command for skipping a form step submission.
    /// </summary>
    public class SkipFormStepSubmissionCommand : IRequest<FormStepSubmissionDto>
    {
        /// <summary>
        /// Gets or sets the unique identifier of the form step submission to skip.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

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

        /// <summary>
        /// Gets or sets the user who is skipping the step submission.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Skipped by cannot exceed 100 characters.")]
        public string SkippedBy { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Tenant ID cannot exceed 100 characters.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically move to the next step after skipping.
        /// </summary>
        public bool AutoMoveToNextStep { get; set; } = true;
    }
}