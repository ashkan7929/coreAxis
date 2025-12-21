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
using CoreAxis.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.FormStepSubmissions
{
    /// <summary>
    /// Query handlers for form step submission operations.
    /// </summary>
    public class FormStepSubmissionQueryHandlers :
        IRequestHandler<GetFormStepSubmissionByIdQuery, FormStepSubmissionDto>,
        IRequestHandler<GetFormStepSubmissionsByFormSubmissionIdQuery, IEnumerable<FormStepSubmissionDto>>,
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
                    throw new EntityNotFoundException(nameof(FormStepSubmission), request.Id);
                }

                // Apply filters
                if (!request.IncludeInactive && !formStepSubmission.IsActive)
                {
                    throw new EntityNotFoundException(nameof(FormStepSubmission), request.Id);
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
        public async Task<IEnumerable<FormStepSubmissionDto>> Handle(GetFormStepSubmissionsByFormSubmissionIdQuery request, CancellationToken cancellationToken)
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

                if (!string.IsNullOrEmpty(request.StatusFilter))
                {
                    filteredSubmissions = filteredSubmissions.Where(s => s.Status == request.StatusFilter);
                }

                if (request.CompletedOnly)
                {
                    filteredSubmissions = filteredSubmissions.Where(s => s.Status == "Completed");
                }

                if (request.IncompleteOnly)
                {
                    filteredSubmissions = filteredSubmissions.Where(s => s.Status != "Completed");
                }

                if (request.SkippedOnly)
                {
                    filteredSubmissions = filteredSubmissions.Where(s => s.Status == "Skipped");
                }

                if (request.OrderByStepNumber)
                {
                    filteredSubmissions = filteredSubmissions.OrderBy(s => s.StepNumber);
                }

                _logger.LogInformation("Found {Count} form step submissions for form submission {FormSubmissionId}", 
                    filteredSubmissions.Count(), request.FormSubmissionId);

                return filteredSubmissions.Select(MapToDto);
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
                _logger.LogInformation("Getting analytics for form {FormId}", request.FormId);

                var analyticsData = await _formStepSubmissionRepository.GetAnalyticsAsync(
                    request.FormId, request.TenantId, request.StartDate, request.EndDate, cancellationToken);

                var result = new FormStepSubmissionAnalyticsDto
                {
                    FormId = request.FormId,
                    // TenantId is Guid in DTO but string in request?
                    // Assuming request.TenantId is parseable Guid or DTO property should be string.
                    // FormStepSubmissionAnalyticsDto.TenantId is Guid.
                    // request.TenantId is string.
                    TenantId = Guid.TryParse(request.TenantId, out var tid) ? tid : Guid.Empty,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate
                };

                var stepAnalyticsList = new List<FormStepAnalyticsDto>();

                foreach (var item in analyticsData)
                {
                    // Map dynamic result to DTO
                    stepAnalyticsList.Add(new FormStepAnalyticsDto
                    {
                        StepNumber = item.StepNumber,
                        StepName = item.StepTitle ?? string.Empty,
                        TotalSubmissions = item.TotalSubmissions,
                        CompletedSubmissions = item.CompletedSubmissions,
                        SkippedSubmissions = item.SkippedSubmissions,
                        // InProgressSubmissions might not be in dynamic object, calculating if possible
                        InProgressSubmissions = item.TotalSubmissions - item.CompletedSubmissions - item.SkippedSubmissions,
                        AverageCompletionTimeSeconds = item.AverageCompletionTimeSeconds,
                        CompletionRate = item.CompletionRate,
                        // DropOffRate might be used as SkipRate or calculated
                        SkipRate = item.DropOffRate // Mapping DropOffRate to SkipRate as approximation
                    });
                }

                // Apply filters
                if (request.MinCompletionRate.HasValue)
                {
                    stepAnalyticsList.RemoveAll(a => (double)a.CompletionRate < request.MinCompletionRate.Value);
                }

                if (request.MaxAverageCompletionTime.HasValue)
                {
                    stepAnalyticsList.RemoveAll(a => a.AverageCompletionTimeSeconds > request.MaxAverageCompletionTime.Value);
                }

                if (request.OrderByStepNumber)
                {
                    stepAnalyticsList.Sort((a, b) => a.StepNumber.CompareTo(b.StepNumber));
                }

                result.StepAnalytics = stepAnalyticsList;
                result.TotalSteps = stepAnalyticsList.Count;
                
                if (stepAnalyticsList.Any())
                {
                    result.OverallCompletionRate = stepAnalyticsList.Average(a => a.CompletionRate);
                    result.AverageFormCompletionTimeSeconds = (int)stepAnalyticsList.Sum(a => a.AverageCompletionTimeSeconds);
                }

                _logger.LogInformation("Analytics for form {FormId} retrieved successfully", request.FormId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics for form {FormId}", request.FormId);
                throw;
            }
        }

        private FormStepSubmissionDto MapToDto(FormStepSubmission entity)
        {
            return new FormStepSubmissionDto
            {
                Id = entity.Id,
                FormSubmissionId = entity.FormSubmissionId,
                FormStepId = entity.FormStepId,
                StepNumber = entity.StepNumber,
                Status = entity.Status,
                StepData = entity.StepData,
                ValidationErrors = entity.ValidationErrors,
                StartedAt = entity.StartedAt,
                CompletedAt = entity.CompletedAt,
                TimeSpentSeconds = entity.TimeSpentSeconds,
                TenantId = entity.TenantId,
                IsActive = entity.IsActive,
                CreatedBy = entity.CreatedBy,
                CreatedOn = entity.CreatedOn,
                LastModifiedBy = entity.LastModifiedBy,
                LastModifiedOn = entity.LastModifiedOn
            };
        }
    }
}