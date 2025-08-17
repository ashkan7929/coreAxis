using System;
using System.Collections.Generic;

namespace CoreAxis.Modules.DynamicForm.Application.DTOs
{
    /// <summary>
    /// DTO for form step submission analytics.
    /// </summary>
    public class FormStepSubmissionAnalyticsDto
    {
        /// <summary>
        /// Gets or sets the form ID.
        /// </summary>
        public Guid FormId { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the start date for analytics.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date for analytics.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the step analytics.
        /// </summary>
        public List<FormStepAnalyticsDto> StepAnalytics { get; set; } = new List<FormStepAnalyticsDto>();

        /// <summary>
        /// Gets or sets the total number of steps.
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// Gets or sets the overall completion rate across all steps.
        /// </summary>
        public decimal OverallCompletionRate { get; set; }

        /// <summary>
        /// Gets or sets the average form completion time in seconds.
        /// </summary>
        public int AverageFormCompletionTimeSeconds { get; set; }
    }

    /// <summary>
    /// DTO for individual form step analytics.
    /// </summary>
    public class FormStepAnalyticsDto
    {
        /// <summary>
        /// Gets or sets the step number.
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// Gets or sets the step name.
        /// </summary>
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of submissions for this step.
        /// </summary>
        public int TotalSubmissions { get; set; }

        /// <summary>
        /// Gets or sets the number of completed submissions.
        /// </summary>
        public int CompletedSubmissions { get; set; }

        /// <summary>
        /// Gets or sets the number of skipped submissions.
        /// </summary>
        public int SkippedSubmissions { get; set; }

        /// <summary>
        /// Gets or sets the number of in-progress submissions.
        /// </summary>
        public int InProgressSubmissions { get; set; }

        /// <summary>
        /// Gets or sets the completion rate (0-100).
        /// </summary>
        public decimal CompletionRate { get; set; }

        /// <summary>
        /// Gets or sets the skip rate (0-100).
        /// </summary>
        public decimal SkipRate { get; set; }

        /// <summary>
        /// Gets or sets the average completion time in seconds.
        /// </summary>
        public int AverageCompletionTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the median completion time in seconds.
        /// </summary>
        public int MedianCompletionTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the minimum completion time in seconds.
        /// </summary>
        public int MinCompletionTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum completion time in seconds.
        /// </summary>
        public int MaxCompletionTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the step is active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}