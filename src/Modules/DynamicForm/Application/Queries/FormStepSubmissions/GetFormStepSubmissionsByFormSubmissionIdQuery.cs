using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Queries.FormStepSubmissions
{
    /// <summary>
    /// Query for retrieving all form step submissions for a specific form submission.
    /// </summary>
    public class GetFormStepSubmissionsByFormSubmissionIdQuery : IRequest<IEnumerable<FormStepSubmissionDto>>
    {
        /// <summary>
        /// Gets or sets the form submission identifier.
        /// </summary>
        [Required]
        public Guid FormSubmissionId { get; set; }

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
        /// Gets or sets a value indicating whether to order submissions by step number.
        /// </summary>
        public bool OrderByStepNumber { get; set; } = true;

        /// <summary>
        /// Gets or sets the status filter for step submissions.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Status filter cannot exceed 50 characters.")]
        public string StatusFilter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include only completed submissions.
        /// </summary>
        public bool CompletedOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to include only incomplete submissions.
        /// </summary>
        public bool IncompleteOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to include only skipped submissions.
        /// </summary>
        public bool SkippedOnly { get; set; } = false;
    }
}