using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Commands.FormStepSubmissions
{
    /// <summary>
    /// Command for updating an existing form step submission.
    /// </summary>
    public class UpdateFormStepSubmissionCommand : IRequest<FormStepSubmissionDto>
    {
        /// <summary>
        /// Gets or sets the unique identifier of the form step submission to update.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the step data as JSON.
        /// </summary>
        public string StepData { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets the user who is updating the step submission.
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