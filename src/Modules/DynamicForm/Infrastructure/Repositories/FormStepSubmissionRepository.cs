using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Repositories;

public class FormStepSubmissionRepository : Repository<FormStepSubmission>, IFormStepSubmissionRepository
{
    public FormStepSubmissionRepository(DynamicFormDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByFormSubmissionIdAsync(Guid formSubmissionId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query
            .OrderBy(fss => fss.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<FormStepSubmission?> GetByFormSubmissionIdAndStepNumberAsync(Guid formSubmissionId, int stepNumber, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && fss.StepNumber == stepNumber);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormStepSubmission?> GetCurrentStepAsync(Guid formSubmissionId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.Status == StepSubmissionStatus.InProgress);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query
            .OrderBy(fss => fss.StepNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormStepSubmission?> GetNextStepAsync(Guid formSubmissionId, int currentStepNumber, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.StepNumber > currentStepNumber &&
                         fss.Status == StepSubmissionStatus.NotStarted);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query
            .OrderBy(fss => fss.StepNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetCompletedStepsAsync(Guid formSubmissionId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.Status == StepSubmissionStatus.Completed);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query
            .OrderBy(fss => fss.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetIncompleteStepsAsync(Guid formSubmissionId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.Status != StepSubmissionStatus.Completed &&
                         fss.Status != StepSubmissionStatus.Skipped);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query
            .OrderBy(fss => fss.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetSkippedStepsAsync(Guid formSubmissionId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.Status == StepSubmissionStatus.Skipped);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query
            .OrderBy(fss => fss.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCompletedStepsCountAsync(Guid formSubmissionId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.Status == StepSubmissionStatus.Completed);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetTotalStepsCountAsync(Guid formSubmissionId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> IsStepCompletedAsync(Guid formSubmissionId, int stepNumber, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.StepNumber == stepNumber &&
                         fss.Status == StepSubmissionStatus.Completed);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> CanMoveToStepAsync(Guid formSubmissionId, int targetStepNumber, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        // Check if all previous steps are completed or skipped
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.StepNumber < targetStepNumber &&
                         fss.Status != StepSubmissionStatus.Completed &&
                         fss.Status != StepSubmissionStatus.Skipped);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<double> GetCompletionPercentageAsync(Guid formSubmissionId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var totalSteps = await GetTotalStepsCountAsync(formSubmissionId, tenantId, cancellationToken);
        if (totalSteps == 0) return 0;

        var completedSteps = await GetCompletedStepsCountAsync(formSubmissionId, tenantId, cancellationToken);
        return (double)completedSteps / totalSteps * 100;
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByStatusAsync(string status, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.Status == status);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query
            .OrderBy(fss => fss.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByUserIdAsync(string userId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            throw new ArgumentException("Invalid user ID format.", nameof(userId));

        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.UserId == userGuid);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query
            .OrderByDescending(fss => fss.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByFormStepIdAsync(Guid formStepId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormStepId == formStepId);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fss => fss.TenantId == tenantId);
        }

        return await query
            .OrderByDescending(fss => fss.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    // Additional interface methods implementation
    public async Task<FormStepSubmission> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.Id == id && fss.TenantId == tenantId.ToString())
            .FirstAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByFormSubmissionIdAsync(Guid formSubmissionId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && fss.TenantId == tenantId.ToString())
            .OrderBy(fss => fss.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<FormStepSubmission> GetByFormSubmissionIdAndStepNumberAsync(Guid formSubmissionId, int stepNumber, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && fss.StepNumber == stepNumber && fss.TenantId == tenantId.ToString())
            .FirstAsync(cancellationToken);
    }

    public async Task<IEnumerable<dynamic>> GetAnalyticsAsync(Guid formId, Guid tenantId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.TenantId == tenantId.ToString());

        if (startDate.HasValue)
            query = query.Where(fss => fss.CreatedOn >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(fss => fss.CreatedOn <= endDate.Value);

        return await query
            .GroupBy(fss => fss.StepNumber)
            .Select(g => new { StepNumber = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByFormSubmissionIdAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await GetByFormSubmissionIdAsync(formSubmissionId, null, cancellationToken);
    }

    public async Task<FormStepSubmission> GetByFormSubmissionIdAndStepNumberAsync(Guid formSubmissionId, int stepNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && fss.StepNumber == stepNumber)
            .FirstAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByFormStepIdAsync(Guid formStepId, CancellationToken cancellationToken = default)
    {
        return await GetByFormStepIdAsync(formStepId, null, cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetByUserIdAsync(userId.ToString(), null, cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync(status, null, cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetIncompleteStepSubmissionsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await GetIncompleteStepsAsync(formSubmissionId, null, cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetCompletedStepSubmissionsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await GetCompletedStepsAsync(formSubmissionId, null, cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetSkippedStepSubmissionsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await GetSkippedStepsAsync(formSubmissionId, null, cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetStepSubmissionsWithErrorsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && !string.IsNullOrEmpty(fss.ValidationErrors))
            .OrderBy(fss => fss.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<FormStepSubmission> GetCurrentStepSubmissionAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        var currentStep = await GetCurrentStepAsync(formSubmissionId, null, cancellationToken);
        return currentStep ?? throw new InvalidOperationException($"No current step found for form submission {formSubmissionId}");
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.CreatedOn >= fromDate && fss.CreatedOn <= toDate)
            .OrderBy(fss => fss.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetSlowStepSubmissionsAsync(int minTimeSeconds, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.CompletedAt.HasValue && fss.StartedAt.HasValue && 
                         (fss.CompletedAt!.Value - fss.StartedAt!.Value).TotalSeconds > minTimeSeconds)
            .OrderByDescending(fss => fss.CompletedAt!.Value - fss.StartedAt!.Value)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.TenantId == tenantId)
            .OrderByDescending(fss => fss.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<StepCompletionAnalytics> GetStepCompletionAnalyticsAsync(Guid formStepId, CancellationToken cancellationToken = default)
    {
        var submissions = await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormStepId == formStepId)
            .ToListAsync(cancellationToken);

        var completedSubmissions = submissions.Where(s => s.Status == StepSubmissionStatus.Completed && s.StartedAt.HasValue && s.CompletedAt.HasValue).ToList();
        var skippedSubmissions = submissions.Where(s => s.Status == StepSubmissionStatus.Skipped).ToList();

        var completionTimes = completedSubmissions
            .Select(s => (s.CompletedAt!.Value - s.StartedAt!.Value).TotalSeconds)
            .ToList();

        return new StepCompletionAnalytics
        {
            AverageCompletionTimeSeconds = completionTimes.Any() ? completionTimes.Average() : 0,
            MinCompletionTimeSeconds = completionTimes.Any() ? (int)completionTimes.Min() : 0,
            MaxCompletionTimeSeconds = completionTimes.Any() ? (int)completionTimes.Max() : 0,
            TotalSubmissions = submissions.Count,
            CompletedSubmissions = completedSubmissions.Count,
            SkippedSubmissions = skippedSubmissions.Count,
            CompletionRate = submissions.Count > 0 ? (double)completedSubmissions.Count / submissions.Count * 100 : 0,
            SkipRate = submissions.Count > 0 ? (double)skippedSubmissions.Count / submissions.Count * 100 : 0
        };
    }
}