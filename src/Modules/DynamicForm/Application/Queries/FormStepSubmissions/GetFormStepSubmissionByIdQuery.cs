using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Queries.FormStepSubmissions
{
    /// <summary>
    /// Query for retrieving a form step submission by its unique identifier.
    /// </summary>
    public class GetFormStepSubmissionByIdQuery : IRequest<FormStepSubmissionDto>
    {
        /// <summary>
        /// Gets or sets the unique identifier of the form step submission.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Tenant ID cannot exceed 100 characters.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include inactive submissions.
        /// </summary>
        public bool IncludeInactive { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to include form step details.
        /// </summary>
        public bool IncludeFormStep { get; set; } = false;
    }
}