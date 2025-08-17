using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Repositories;

public class FormStepRepository : Repository<FormStep>, IFormStepRepository
{
    public FormStepRepository(DynamicFormDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<FormStep>> GetByFormIdAsync(Guid formId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<FormStep?> GetByFormIdAndStepNumberAsync(Guid formId, int stepNumber, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.StepNumber == stepNumber);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormStep?> GetFirstStepAsync(Guid formId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderBy(fs => fs.StepNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormStep?> GetLastStepAsync(Guid formId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderByDescending(fs => fs.StepNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormStep?> GetNextStepAsync(Guid formId, int currentStepNumber, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && 
                        fs.StepNumber > currentStepNumber && 
                        fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderBy(fs => fs.StepNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormStep?> GetPreviousStepAsync(Guid formId, int currentStepNumber, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && 
                        fs.StepNumber < currentStepNumber && 
                        fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderByDescending(fs => fs.StepNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetRequiredStepsAsync(Guid formId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.IsRequired && fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetSkippableStepsAsync(Guid formId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.IsSkippable && fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetByStepTypeAsync(string stepType, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.StepType == stepType && fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderBy(fs => fs.FormId)
            .ThenBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetStepsCountAsync(Guid formId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> StepExistsAsync(Guid formId, int stepNumber, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.StepNumber == stepNumber && fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsFirstStepAsync(Guid formId, int stepNumber, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var firstStep = await GetFirstStepAsync(formId, tenantId, cancellationToken);
        return firstStep?.StepNumber == stepNumber;
    }

    public async Task<bool> IsLastStepAsync(Guid formId, int stepNumber, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var lastStep = await GetLastStepAsync(formId, tenantId, cancellationToken);
        return lastStep?.StepNumber == stepNumber;
    }

    public async Task<IEnumerable<FormStep>> GetStepsWithDependenciesAsync(Guid formId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && 
                        !string.IsNullOrEmpty(fs.DependsOnSteps) && 
                        fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetConditionalStepsAsync(Guid formId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && 
                        !string.IsNullOrEmpty(fs.ConditionalLogic) && 
                        fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetRepeatableStepsAsync(Guid formId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.IsRepeatable && fs.IsActive);

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        return await query
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task ReorderStepsAsync(Guid formId, Dictionary<Guid, int> stepOrderMap, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && stepOrderMap.Keys.Contains(fs.Id));

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(fs => fs.TenantId == tenantId);
        }

        var steps = await query.ToListAsync(cancellationToken);

        foreach (var step in steps)
        {
            if (stepOrderMap.TryGetValue(step.Id, out var newStepNumber))
            {
                step.StepNumber = newStepNumber;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    // Additional interface methods implementation
    public async Task<FormStep> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.Id == id && fs.TenantId == tenantId.ToString())
            .FirstAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetByFormIdAsync(Guid formId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.TenantId == tenantId.ToString())
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetByFormIdAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.IsActive)
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<FormStep> GetByFormIdAndStepNumberAsync(Guid formId, int stepNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.StepNumber == stepNumber)
            .FirstAsync(cancellationToken);
    }

    public async Task<int> GetMaxStepNumberAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        var maxStep = await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId)
            .MaxAsync(fs => (int?)fs.StepNumber, cancellationToken);
        return maxStep ?? 0;
    }

    public async Task<IEnumerable<FormStep>> GetStepsWithConditionalDependencyAsync(Guid formId, string fieldName, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && 
                        !string.IsNullOrEmpty(fs.ConditionalLogic) && 
                        fs.ConditionalLogic.Contains(fieldName) &&
                        fs.IsActive)
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetRequiredStepsAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.IsRequired && fs.IsActive)
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetSkippableStepsAsync(Guid formId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.CanSkip && fs.IsActive)
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetStepsInRangeAsync(Guid formId, int fromStepNumber, int toStepNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && 
                        fs.StepNumber >= fromStepNumber && 
                        fs.StepNumber <= toStepNumber &&
                        fs.IsActive)
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> StepNumberExistsAsync(Guid formId, int stepNumber, Guid? excludeStepId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.StepNumber == stepNumber);

        if (excludeStepId.HasValue)
        {
            query = query.Where(fs => fs.Id != excludeStepId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.TenantId == tenantId)
            .OrderBy(fs => fs.FormId)
            .ThenBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }
}