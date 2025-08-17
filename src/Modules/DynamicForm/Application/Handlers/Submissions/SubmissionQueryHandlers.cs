using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Application.Queries.Submissions;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.Submissions;

public class GetSubmissionByIdQueryHandler : IRequestHandler<GetSubmissionByIdQuery, Result<FormSubmissionDto>>
{
    private readonly IFormSubmissionRepository _submissionRepository;
    private readonly ILogger<GetSubmissionByIdQueryHandler> _logger;

    public GetSubmissionByIdQueryHandler(
        IFormSubmissionRepository submissionRepository,
        ILogger<GetSubmissionByIdQueryHandler> logger)
    {
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public async Task<Result<FormSubmissionDto>> Handle(GetSubmissionByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var submission = await _submissionRepository.GetByIdWithIncludesAsync(
                request.Id,
                includeForm: request.IncludeForm,
                cancellationToken: cancellationToken);

            if (submission == null)
            {
                return Result<FormSubmissionDto>.Failure($"Submission with ID {request.Id} not found.");
            }

            var submissionDto = MapToDto(submission, request.IncludeForm);
            return Result<FormSubmissionDto>.Success(submissionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submission: {SubmissionId}", request.Id);
            return Result<FormSubmissionDto>.Failure($"Error retrieving submission: {ex.Message}");
        }
    }

    private static FormSubmissionDto MapToDto(FormSubmission submission, bool includeForm = false)
    {
        var dto = new FormSubmissionDto
        {
            Id = submission.Id,
            FormId = submission.FormId,
            SubmissionData = submission.SubmissionData,
            UserId = submission.UserId,
            SessionId = submission.SessionId,
            IpAddress = submission.IpAddress,
            UserAgent = submission.UserAgent,
            Status = submission.Status,
            Metadata = submission.Metadata,
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt
        };

        if (includeForm && submission.Form != null)
        {
            dto.Form = new FormDto
            {
                Id = submission.Form.Id,
                Name = submission.Form.Name,
                Description = submission.Form.Description,
                SchemaJson = submission.Form.SchemaJson,
                IsActive = submission.Form.IsActive,
                TenantId = submission.Form.TenantId,
                BusinessId = submission.Form.BusinessId,
                Metadata = submission.Form.Metadata,
                CreatedAt = submission.Form.CreatedAt,
                UpdatedAt = submission.Form.UpdatedAt,
                CreatedBy = submission.Form.CreatedBy,
                UpdatedBy = submission.Form.UpdatedBy,
                Version = submission.Form.Version
            };
        }

        return dto;
    }
}

public class GetSubmissionsQueryHandler : IRequestHandler<GetSubmissionsQuery, Result<PagedResult<FormSubmissionDto>>>
{
    private readonly IFormSubmissionRepository _submissionRepository;
    private readonly ILogger<GetSubmissionsQueryHandler> _logger;

    public GetSubmissionsQueryHandler(
        IFormSubmissionRepository submissionRepository,
        ILogger<GetSubmissionsQueryHandler> logger)
    {
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<FormSubmissionDto>>> Handle(GetSubmissionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (submissions, totalCount) = await _submissionRepository.GetPagedAsync(
                request.FormId,
                request.UserId,
                request.Status,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize,
                request.IncludeForm,
                cancellationToken);

            var submissionDtos = submissions.Select(s => MapToDto(s, request.IncludeForm)).ToList();

            var pagedResult = new PagedResult<FormSubmissionDto>
            {
                Items = submissionDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Result<PagedResult<FormSubmissionDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submissions with filters: FormId: {FormId}, UserId: {UserId}", request.FormId, request.UserId);
            return Result<PagedResult<FormSubmissionDto>>.Failure($"Error retrieving submissions: {ex.Message}");
        }
    }

    private static FormSubmissionDto MapToDto(FormSubmission submission, bool includeForm = false)
    {
        var dto = new FormSubmissionDto
        {
            Id = submission.Id,
            FormId = submission.FormId,
            SubmissionData = submission.SubmissionData,
            UserId = submission.UserId,
            SessionId = submission.SessionId,
            IpAddress = submission.IpAddress,
            UserAgent = submission.UserAgent,
            Status = submission.Status,
            Metadata = submission.Metadata,
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt
        };

        if (includeForm && submission.Form != null)
        {
            dto.Form = new FormDto
            {
                Id = submission.Form.Id,
                Name = submission.Form.Name,
                Description = submission.Form.Description,
                SchemaJson = submission.Form.SchemaJson,
                IsActive = submission.Form.IsActive,
                TenantId = submission.Form.TenantId,
                BusinessId = submission.Form.BusinessId,
                Metadata = submission.Form.Metadata,
                CreatedAt = submission.Form.CreatedAt,
                UpdatedAt = submission.Form.UpdatedAt,
                CreatedBy = submission.Form.CreatedBy,
                UpdatedBy = submission.Form.UpdatedBy,
                Version = submission.Form.Version
            };
        }

        return dto;
    }
}

public class GetSubmissionsByFormQueryHandler : IRequestHandler<GetSubmissionsByFormQuery, Result<PagedResult<FormSubmissionDto>>>
{
    private readonly IFormSubmissionRepository _submissionRepository;
    private readonly ILogger<GetSubmissionsByFormQueryHandler> _logger;

    public GetSubmissionsByFormQueryHandler(
        IFormSubmissionRepository submissionRepository,
        ILogger<GetSubmissionsByFormQueryHandler> logger)
    {
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<FormSubmissionDto>>> Handle(GetSubmissionsByFormQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (submissions, totalCount) = await _submissionRepository.GetByFormIdPagedAsync(
                request.FormId,
                request.UserId,
                request.Status,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize,
                request.IncludeForm,
                cancellationToken);

            var submissionDtos = submissions.Select(s => MapToDto(s, request.IncludeForm)).ToList();

            var pagedResult = new PagedResult<FormSubmissionDto>
            {
                Items = submissionDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return Result<PagedResult<FormSubmissionDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submissions for form: {FormId}", request.FormId);
            return Result<PagedResult<FormSubmissionDto>>.Failure($"Error retrieving submissions for form: {ex.Message}");
        }
    }

    private static FormSubmissionDto MapToDto(FormSubmission submission, bool includeForm = false)
    {
        var dto = new FormSubmissionDto
        {
            Id = submission.Id,
            FormId = submission.FormId,
            SubmissionData = submission.SubmissionData,
            UserId = submission.UserId,
            SessionId = submission.SessionId,
            IpAddress = submission.IpAddress,
            UserAgent = submission.UserAgent,
            Status = submission.Status,
            Metadata = submission.Metadata,
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt
        };

        if (includeForm && submission.Form != null)
        {
            dto.Form = new FormDto
            {
                Id = submission.Form.Id,
                Name = submission.Form.Name,
                Description = submission.Form.Description,
                SchemaJson = submission.Form.SchemaJson,
                IsActive = submission.Form.IsActive,
                TenantId = submission.Form.TenantId,
                BusinessId = submission.Form.BusinessId,
                Metadata = submission.Form.Metadata,
                CreatedAt = submission.Form.CreatedAt,
                UpdatedAt = submission.Form.UpdatedAt,
                CreatedBy = submission.Form.CreatedBy,
                UpdatedBy = submission.Form.UpdatedBy,
                Version = submission.Form.Version
            };
        }

        return dto;
    }
}

public class GetSubmissionStatsQueryHandler : IRequestHandler<GetSubmissionStatsQuery, Result<SubmissionStatsDto>>
{
    private readonly IFormSubmissionRepository _submissionRepository;
    private readonly ILogger<GetSubmissionStatsQueryHandler> _logger;

    public GetSubmissionStatsQueryHandler(
        IFormSubmissionRepository submissionRepository,
        ILogger<GetSubmissionStatsQueryHandler> logger)
    {
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public async Task<Result<SubmissionStatsDto>> Handle(GetSubmissionStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _submissionRepository.GetStatsAsync(
                request.FormId,
                request.FromDate,
                request.ToDate,
                cancellationToken);

            if (stats == null)
            {
                return Result<SubmissionStatsDto>.Failure($"Unable to retrieve stats for form: {request.FormId}");
            }

            var statsDto = new SubmissionStatsDto
            {
                FormId = stats.FormId,
                TotalSubmissions = stats.TotalSubmissions,
                SubmissionsToday = stats.SubmissionsToday,
                SubmissionsThisWeek = stats.SubmissionsThisWeek,
                SubmissionsThisMonth = stats.SubmissionsThisMonth,
                FirstSubmissionDate = stats.FirstSubmissionDate,
                LastSubmissionDate = stats.LastSubmissionDate,
                SubmissionsByStatus = stats.SubmissionsByStatus
            };

            return Result<SubmissionStatsDto>.Success(statsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submission stats for form: {FormId}", request.FormId);
            return Result<SubmissionStatsDto>.Failure($"Error retrieving submission stats: {ex.Message}");
        }
    }
}