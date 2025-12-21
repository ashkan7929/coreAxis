using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Repositories;

public class FormStepRepository : Repository<FormStep>, IFormStepRepository
{
    public FormStepRepository(DynamicFormDbContext context) : base(context)
    {
    }

    public async Task<FormStep?> GetByIdAsync(Guid id, string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.Id == id && fs.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetByFormIdAsync(Guid formId, string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.TenantId == tenantId)
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

    public async Task<FormStep?> GetByFormIdAndStepNumberAsync(Guid formId, int stepNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.StepNumber == stepNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormStep?> GetByFormIdAndStepNumberAsync(Guid formId, int stepNumber, string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.StepNumber == stepNumber && fs.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);
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
            .Where(fs => fs.FormId == formId && fs.ConditionalLogic.Contains(fieldName))
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
            .Where(fs => fs.FormId == formId && fs.IsSkippable && fs.IsActive)
            .OrderBy(fs => fs.StepNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormStep>> GetStepsInRangeAsync(Guid formId, int fromStepNumber, int toStepNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormStep>()
            .Where(fs => fs.FormId == formId && fs.StepNumber >= fromStepNumber && fs.StepNumber <= toStepNumber)
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
            .ToListAsync(cancellationToken);
    }
}