using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CoreAxis.Modules.DynamicForm.Application.DTOs;

namespace CoreAxis.Modules.DynamicForm.Application.Queries.FormStepSubmissions
{
    /// <summary>
    /// Query for retrieving analytics data for form step submissions.
    /// </summary>
    public class GetFormStepSubmissionAnalyticsQuery : IRequest<IEnumerable<FormStepSubmissionAnalyticsDto>>
    {
        /// <summary>
        /// Gets or sets the form identifier to get analytics for.
        /// </summary>
        [Required]
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenancy support.
        /// </summary>
        [Required]
        [MaxLength(100, ErrorMessage = "Tenant ID cannot exceed 100 characters.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the start date for the analytics period.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date for the analytics period.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include only active steps.
        /// </summary>
        public bool ActiveStepsOnly { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to order results by step number.
        /// </summary>
        public bool OrderByStepNumber { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum completion rate threshold (0-100).
        /// </summary>
        [Range(0, 100, ErrorMessage = "Completion rate threshold must be between 0 and 100.")]
        public double? MinCompletionRate { get; set; }

        /// <summary>
        /// Gets or sets the maximum average completion time in seconds.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Maximum completion time must be non-negative.")]
        public int? MaxAverageCompletionTime { get; set; }
    }
}