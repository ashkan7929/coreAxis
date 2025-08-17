using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Application.Queries.FormStepSubmissions;
using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.BuildingBlocks.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.FormStepSubmissions
{
    /// <summary>
    /// Query handlers for form step submission operations.
    /// </summary>
    public class FormStepSubmissionQueryHandlers :
        IRequestHandler<GetFormStepSubmissionByIdQuery, FormStepSubmissionDto>,
        IRequestHandler<GetFormStepSubmissionsByFormSubmissionIdQuery, List<FormStepSubmissionDto>>,
        IRequestHandler<GetFormStepSubmissionAnalyticsQuery, FormStepSubmissionAnalyticsDto>
    {
        private readonly IFormStepSubmissionRepository _formStepSubmissionRepository;
        private readonly ILogger<FormStepSubmissionQueryHandlers> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormStepSubmissionQueryHandlers"/> class.
        /// </summary>
        /// <param name="formStepSubmissionRepository">The form step submission repository.</param>
        /// <param name="logger">The logger.</param>
        public FormStepSubmissionQueryHandlers(
            IFormStepSubmissionRepository formStepSubmissionRepository,
            ILogger<FormStepSubmissionQueryHandlers> logger)
        {
            _formStepSubmissionRepository = formStepSubmissionRepository ?? throw new ArgumentNullException(nameof(formStepSubmissionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the get form step submission by ID query.
        /// </summary>
        /// <param name="request">The get form step submission by ID query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form step submission DTO.</returns>
        public async Task<FormStepSubmissionDto> Handle(GetFormStepSubmissionByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting form step submission by ID {SubmissionId}", request.Id);

                var formStepSubmission = await _formStepSubmissionRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken);
                
                if (formStepSubmission == null)
                {
                    throw new NotFoundException($"Form step submission with ID {request.Id} not found");
                }

                // Apply filters
                if (!request.IncludeInactive && !formStepSubmission.IsActive)
                {
                    throw new NotFoundException($"Form step submission with ID {request.Id} not found");
                }

                _logger.LogInformation("Form step submission {SubmissionId} retrieved successfully", request.Id);

                return MapToDto(formStepSubmission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting form step submission by ID {SubmissionId}", request.Id);
                throw;
            }
        }

        /// <summary>
        /// Handles the get form step submissions by form submission ID query.
        /// </summary>
        /// <param name="request">The get form step submissions by form submission ID query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of form step submission DTOs.</returns>
        public async Task<List<FormStepSubmissionDto>> Handle(GetFormStepSubmissionsByFormSubmissionIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting form step submissions for form submission {FormSubmissionId}", request.FormSubmissionId);

                var formStepSubmissions = await _formStepSubmissionRepository.GetByFormSubmissionIdAsync(
                    request.FormSubmissionId, request.TenantId, cancellationToken);

                // Apply filters
                var filteredSubmissions = formStepSubmissions.AsQueryable();

                if (!request.IncludeInactive)
                {
                    filteredSubmissions = filteredSubmissions.Where(s => s.IsActive);
                }

                if (request.StatusFilter.HasValue)
                {
                    filteredSubmissions = filteredSubmissions.Where(s => s.Status == request.StatusFilter.Value);
                }

                if (request.CompletedOnly)
                {
                    filteredSubmissions = filteredSubmissions.Where(s => s.Status == StepSubmissionStatus.Completed);
                }

                if (request.IncompleteOnly)
                {
                    filteredSubmissions = filteredSubmissions.Where(s => s.Status != StepSubmissionStatus.Completed && !s.IsSkipped);
                }

                if (request.SkippedOnly)
                {
                    filteredSubmissions = filteredSubmissions.Where(s => s.IsSkipped);
                }

                // Apply ordering
                if (request.OrderByStepNumber)
                {
                    filteredSubmissions = filteredSubmissions.OrderBy(s => s.StepNumber);
                }
                else
                {
                    filteredSubmissions = filteredSubmissions.OrderBy(s => s.CreatedOn);
                }

                var result = filteredSubmissions.ToList();

                _logger.LogInformation("Retrieved {Count} form step submissions for form submission {FormSubmissionId}", 
                    result.Count, request.FormSubmissionId);

                return result.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting form step submissions for form submission {FormSubmissionId}", request.FormSubmissionId);
                throw;
            }
        }

        /// <summary>
        /// Handles the get form step submission analytics query.
        /// </summary>
        /// <param name="request">The get form step submission analytics query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form step submission analytics DTO.</returns>
        public async Task<FormStepSubmissionAnalyticsDto> Handle(GetFormStepSubmissionAnalyticsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting form step submission analytics for form {FormId}", request.FormId);

                var analytics = await _formStepSubmissionRepository.GetAnalyticsAsync(
                    request.FormId,
                    request.TenantId,
                    request.StartDate,
                    request.EndDate,
                    cancellationToken);

                // Apply filters
                var filteredAnalytics = analytics.AsQueryable();

                if (request.ActiveStepsOnly)
                {
                    filteredAnalytics = filteredAnalytics.Where(a => a.IsActive);
                }

                if (request.MinCompletionRate.HasValue)
                {
                    filteredAnalytics = filteredAnalytics.Where(a => a.CompletionRate >= request.MinCompletionRate.Value);
                }

                if (request.MaxAverageCompletionTime.HasValue)
                {
                    filteredAnalytics = filteredAnalytics.Where(a => a.AverageCompletionTimeSeconds <= request.MaxAverageCompletionTime.Value);
                }

                // Apply ordering
                if (request.OrderByStepNumber)
                {
                    filteredAnalytics = filteredAnalytics.OrderBy(a => a.StepNumber);
                }
                else
                {
                    filteredAnalytics = filteredAnalytics.OrderByDescending(a => a.CompletionRate);
                }

                var result = filteredAnalytics.ToList();

                _logger.LogInformation("Retrieved analytics for {Count} form steps for form {FormId}", 
                    result.Count, request.FormId);

                return new FormStepSubmissionAnalyticsDto
                {
                    FormId = request.FormId,
                    TenantId = request.TenantId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    StepAnalytics = result.Select(MapToStepAnalyticsDto).ToList(),
                    TotalSteps = result.Count,
                    OverallCompletionRate = result.Any() ? result.Average(a => a.CompletionRate) : 0,
                    AverageFormCompletionTimeSeconds = result.Any() ? result.Sum(a => a.AverageCompletionTimeSeconds) : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting form step submission analytics for form {FormId}", request.FormId);
                throw;
            }
        }

        /// <summary>
        /// Maps a form step submission entity to a DTO.
        /// </summary>
        /// <param name="formStepSubmission">The form step submission entity.</param>
        /// <returns>The form step submission DTO.</returns>
        private static FormStepSubmissionDto MapToDto(FormStepSubmission formStepSubmission)
        {
            return new FormStepSubmissionDto
            {
                Id = formStepSubmission.Id,
                FormSubmissionId = formStepSubmission.FormSubmissionId,
                FormStepId = formStepSubmission.FormStepId,
                StepNumber = formStepSubmission.StepNumber,
                UserId = formStepSubmission.UserId,
                TenantId = formStepSubmission.TenantId,
                StepData = formStepSubmission.StepData,
                Status = formStepSubmission.Status,
                ValidationErrors = formStepSubmission.ValidationErrors,
                StartedAt = formStepSubmission.StartedAt,
                CompletedAt = formStepSubmission.CompletedAt,
                TimeSpentSeconds = formStepSubmission.TimeSpentSeconds,
                IsSkipped = formStepSubmission.IsSkipped,
                SkipReason = formStepSubmission.SkipReason,
                Metadata = formStepSubmission.Metadata,
                CreatedOn = formStepSubmission.CreatedOn,
                CreatedBy = formStepSubmission.CreatedBy,
                LastModifiedOn = formStepSubmission.LastModifiedOn,
                LastModifiedBy = formStepSubmission.LastModifiedBy,
                IsActive = formStepSubmission.IsActive
            };
        }

        /// <summary>
        /// Maps a form step submission analytics entity to a DTO.
        /// </summary>
        /// <param name="analytics">The form step submission analytics entity.</param>
        /// <returns>The form step submission analytics DTO.</returns>
        private static FormStepAnalyticsDto MapToStepAnalyticsDto(dynamic analytics)
        {
            return new FormStepAnalyticsDto
            {
                StepNumber = analytics.StepNumber,
                StepName = analytics.StepName,
                TotalSubmissions = analytics.TotalSubmissions,
                CompletedSubmissions = analytics.CompletedSubmissions,
                SkippedSubmissions = analytics.SkippedSubmissions,
                InProgressSubmissions = analytics.InProgressSubmissions,
                CompletionRate = analytics.CompletionRate,
                SkipRate = analytics.SkipRate,
                AverageCompletionTimeSeconds = analytics.AverageCompletionTimeSeconds,
                MedianCompletionTimeSeconds = analytics.MedianCompletionTimeSeconds,
                MinCompletionTimeSeconds = analytics.MinCompletionTimeSeconds,
                MaxCompletionTimeSeconds = analytics.MaxCompletionTimeSeconds,
                IsActive = analytics.IsActive
            };
        }
    }
}