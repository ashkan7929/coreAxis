using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Repositories;

public class FormStepSubmissionRepository : Repository<FormStepSubmission>, IFormStepSubmissionRepository
{
    public FormStepSubmissionRepository(DynamicFormDbContext context) : base(context)
    {
    }

    public async Task<FormStepSubmission?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.Id == id && fss.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByFormSubmissionIdAsync(Guid formSubmissionId, string tenantId, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && fss.TenantId == tenantId);

        return await query
            .OrderBy(fss => fss.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByFormSubmissionIdAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId)
            .OrderBy(fss => fss.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<FormStepSubmission?> GetByFormSubmissionIdAndStepNumberAsync(Guid formSubmissionId, int stepNumber, string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && fss.StepNumber == stepNumber && fss.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormStepSubmission?> GetByFormSubmissionIdAndStepNumberAsync(Guid formSubmissionId, int stepNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && fss.StepNumber == stepNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<dynamic>> GetAnalyticsAsync(Guid formId, string tenantId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new List<dynamic>());
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByFormStepIdAsync(Guid formStepId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormStepId == formStepId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.UserId == userId)
            .OrderByDescending(fss => fss.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetIncompleteStepSubmissionsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.Status != StepSubmissionStatus.Completed && 
                         fss.Status != StepSubmissionStatus.Skipped)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetCompletedStepSubmissionsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.Status == StepSubmissionStatus.Completed)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetSkippedStepSubmissionsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.Status == StepSubmissionStatus.Skipped)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetStepSubmissionsWithErrorsAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         !string.IsNullOrEmpty(fss.ValidationErrors))
            .ToListAsync(cancellationToken);
    }

    public async Task<FormStepSubmission?> GetCurrentStepSubmissionAsync(Guid formSubmissionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormSubmissionId == formSubmissionId && 
                         fss.Status == StepSubmissionStatus.InProgress)
            .OrderByDescending(fss => fss.LastModifiedOn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.CreatedOn >= fromDate && fss.CreatedOn <= toDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetSlowStepSubmissionsAsync(int minTimeSeconds, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.TimeSpentSeconds.HasValue && fss.TimeSpentSeconds > minTimeSeconds)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStepSubmission>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStepSubmission>()
            .Where(fss => fss.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<StepCompletionAnalytics> GetStepCompletionAnalyticsAsync(Guid formStepId, CancellationToken cancellationToken = default)
    {
        var submissions = await _context.Set<FormStepSubmission>()
            .Where(fss => fss.FormStepId == formStepId)
            .ToListAsync(cancellationToken);

        if (!submissions.Any())
        {
            return new StepCompletionAnalytics();
        }

        var completed = submissions.Where(s => s.Status == StepSubmissionStatus.Completed).ToList();
        var skipped = submissions.Where(s => s.Status == StepSubmissionStatus.Skipped).ToList();

        var times = completed.Where(s => s.TimeSpentSeconds.HasValue).Select(s => s.TimeSpentSeconds.Value).ToList();

        return new StepCompletionAnalytics
        {
            TotalSubmissions = submissions.Count,
            CompletedSubmissions = completed.Count,
            SkippedSubmissions = skipped.Count,
            CompletionRate = (double)completed.Count / submissions.Count * 100,
            SkipRate = (double)skipped.Count / submissions.Count * 100,
            AverageCompletionTimeSeconds = times.Any() ? times.Average() : 0,
            MinCompletionTimeSeconds = times.Any() ? times.Min() : 0,
            MaxCompletionTimeSeconds = times.Any() ? times.Max() : 0
        };
    }
}