using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Repositories;

public class FormulaVersionRepository : Repository<FormulaVersion>, IFormulaVersionRepository
{
    public FormulaVersionRepository(DynamicFormDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<FormulaVersion>> GetByFormulaDefinitionIdAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Where(v => v.FormulaDefinitionId == formulaDefinitionId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<FormulaVersion?> GetByFormulaDefinitionIdAndVersionAsync(Guid formulaDefinitionId, int versionNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .FirstOrDefaultAsync(v => v.FormulaDefinitionId == formulaDefinitionId && v.VersionNumber == versionNumber, cancellationToken);
    }

    public async Task<FormulaVersion?> GetActiveVersionAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .FirstOrDefaultAsync(v => v.FormulaDefinitionId == formulaDefinitionId && v.IsActive, cancellationToken);
    }

    public async Task<FormulaVersion?> GetLatestVersionAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Where(v => v.FormulaDefinitionId == formulaDefinitionId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FormulaVersion?> GetLatestPublishedEffectiveVersionAsync(Guid formulaDefinitionId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Where(v => v.FormulaDefinitionId == formulaDefinitionId
                        && v.IsPublished
                        && (v.EffectiveFrom == null || v.EffectiveFrom <= asOfDate)
                        && (v.EffectiveTo == null || v.EffectiveTo >= asOfDate))
            .OrderByDescending(v => v.IsActive)
            .ThenByDescending(v => v.EffectiveFrom)
            .ThenByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetPublishedVersionsAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Where(v => v.FormulaDefinitionId == formulaDefinitionId && v.IsPublished)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetActiveVersionsByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && v.IsActive)
            .OrderBy(v => v.FormulaDefinition.Name)
            .ThenBy(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetVersionsWithErrorsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && !string.IsNullOrEmpty(v.LastError))
            .OrderByDescending(v => v.LastErrorAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetVersionsByPerformanceAsync(string tenantId, bool ascending = true, int take = 10, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && v.AverageExecutionTime.HasValue);

        query = ascending
            ? query.OrderBy(v => v.AverageExecutionTime)
            : query.OrderByDescending(v => v.AverageExecutionTime);

        return await query.Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetStaleVersionsAsync(string tenantId, TimeSpan threshold, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - threshold;
        
        return await _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && 
                       (v.LastExecutedAt == null || v.LastExecutedAt < cutoffDate))
            .OrderBy(v => v.LastExecutedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextVersionNumberAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default)
    {
        var maxVersion = await _context.Set<FormulaVersion>()
            .Where(v => v.FormulaDefinitionId == formulaDefinitionId)
            .MaxAsync(v => (int?)v.VersionNumber, cancellationToken);

        return (maxVersion ?? 0) + 1;
    }

    public async Task<bool> VersionExistsAsync(Guid formulaDefinitionId, int versionNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .AnyAsync(v => v.FormulaDefinitionId == formulaDefinitionId && v.VersionNumber == versionNumber, cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetVersionsByDateRangeAsync(string tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && 
                       v.CreatedOn >= startDate && 
                       v.CreatedOn <= endDate)
            .OrderByDescending(v => v.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetVersionsByExecutionCountAsync(string tenantId, int minExecutions, int? maxExecutions = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && v.ExecutionCount >= minExecutions);

        if (maxExecutions.HasValue)
        {
            query = query.Where(v => v.ExecutionCount <= maxExecutions.Value);
        }

        return await query
            .OrderByDescending(v => v.ExecutionCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetVersionsByDependencyAsync(string tenantId, string dependency, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && 
                       !string.IsNullOrEmpty(v.Dependencies) && 
                       v.Dependencies.Contains(dependency))
            .OrderBy(v => v.FormulaDefinition.Name)
            .ThenBy(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, (int ExecutionCount, double? AverageTime, DateTime? LastExecuted)>> GetExecutionStatisticsAsync(IEnumerable<Guid> versionIds, CancellationToken cancellationToken = default)
    {
        var statistics = await _context.Set<FormulaVersion>()
            .Where(v => versionIds.Contains(v.Id))
            .Select(v => new
            {
                v.Id,
                v.ExecutionCount,
                v.AverageExecutionTime,
                v.LastExecutedAt
            })
            .ToListAsync(cancellationToken);

        return statistics.ToDictionary(
            s => s.Id,
            s => (s.ExecutionCount, s.AverageExecutionTime, s.LastExecutedAt)
        );
    }

    public async Task BulkUpdateExecutionStatisticsAsync(Dictionary<Guid, (int ExecutionCount, double ExecutionTime, bool Success, string? Error)> statistics, CancellationToken cancellationToken = default)
    {
        var versionIds = statistics.Keys.ToList();
        var versions = await _context.Set<FormulaVersion>()
            .Where(v => versionIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        foreach (var version in versions)
        {
            if (statistics.TryGetValue(version.Id, out var stats))
            {
                version.RecordExecution(stats.ExecutionTime, stats.Success, stats.Error);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetDeletableVersionsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && !v.IsActive && !v.IsPublished)
            .OrderBy(v => v.FormulaDefinition.Name)
            .ThenBy(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<FormulaVersion> Versions, int TotalCount)> GetVersionHistoryAsync(Guid formulaDefinitionId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FormulaVersion>()
            .Where(v => v.FormulaDefinitionId == formulaDefinitionId)
            .OrderByDescending(v => v.VersionNumber);

        var totalCount = await query.CountAsync(cancellationToken);
        var versions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (versions, totalCount);
    }

    public async Task<IEnumerable<FormulaVersion>> SearchByExpressionAsync(string tenantId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && 
                       v.Expression.Contains(searchTerm))
            .OrderBy(v => v.FormulaDefinition.Name)
            .ThenByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetVersionsByPublisherAsync(string tenantId, Guid publishedBy, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && v.PublishedBy == publishedBy)
            .OrderByDescending(v => v.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, (double SuccessRate, double? AverageExecutionTime, int TotalExecutions)>> GetPerformanceMetricsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var versions = await _context.Set<FormulaVersion>()
            .Include(v => v.EvaluationLogs)
            .Where(v => v.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return versions.ToDictionary(
            v => v.Id,
            v => (v.GetSuccessRate(), v.AverageExecutionTime, v.ExecutionCount)
        );
    }

    public async Task DeactivateAllVersionsAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default)
    {
        var activeVersions = await _context.Set<FormulaVersion>()
            .Where(v => v.FormulaDefinitionId == formulaDefinitionId && v.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var version in activeVersions)
        {
            version.Deactivate();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetVersionsNeedingOptimizationAsync(string tenantId, double thresholdMs = 1000, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId && 
                       v.AverageExecutionTime.HasValue && 
                       v.AverageExecutionTime > thresholdMs)
            .OrderByDescending(v => v.AverageExecutionTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FormulaVersion>> GetMostExecutedVersionsAsync(string tenantId, int take = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .Include(v => v.FormulaDefinition)
            .Where(v => v.TenantId == tenantId)
            .OrderByDescending(v => v.ExecutionCount)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task ClearOldExecutionHistoryAsync(string tenantId, DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var versions = await _context.Set<FormulaVersion>()
            .Where(v => v.TenantId == tenantId && 
                       v.LastExecutedAt.HasValue && 
                       v.LastExecutedAt < olderThan)
            .ToListAsync(cancellationToken);

        foreach (var version in versions)
        {
            version.ClearExecutionHistory();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasActiveVersionAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FormulaVersion>()
            .AnyAsync(v => v.FormulaDefinitionId == formulaDefinitionId && v.IsActive, cancellationToken);
    }

    public async Task UpdateAsync(FormulaVersion formulaVersion, CancellationToken cancellationToken = default)
    {
        _context.Set<FormulaVersion>().Update(formulaVersion);
        await _context.SaveChangesAsync(cancellationToken);
    }
}