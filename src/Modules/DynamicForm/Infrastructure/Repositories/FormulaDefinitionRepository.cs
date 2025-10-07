using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Repositories
{
    public class FormulaDefinitionRepository : IFormulaDefinitionRepository
    {
        private readonly DynamicFormDbContext _context;

        public FormulaDefinitionRepository(DynamicFormDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<FormulaDefinition> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormulaDefinition>().FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<FormulaDefinition> GetByIdWithLogsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormulaDefinition>()
                .Include(f => f.Versions)
                .Include(f => f.EvaluationLogs)
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        public async Task<FormulaDefinition> GetByNameAsync(string name, string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormulaDefinition>()
                .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.Name == name, cancellationToken);
        }

        public async Task<FormulaDefinition> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            // Fallback across tenants; prefer most recently modified
            return await _context.Set<FormulaDefinition>()
                .OrderByDescending(f => f.LastModifiedOn)
                .ThenByDescending(f => f.CreatedOn)
                .FirstOrDefaultAsync(f => f.Name == name, cancellationToken);
        }

        public async Task<IEnumerable<FormulaDefinition>> GetByTenantAsync(string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormulaDefinition>().Where(f => f.TenantId == tenantId);
            if (!includeInactive)
            {
                query = query.Where(f => f.IsActive);
            }
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormulaDefinition>> GetPublishedByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormulaDefinition>()
                .Where(f => f.TenantId == tenantId && f.IsPublished)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormulaDefinition>> GetByCategoryAsync(string category, string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormulaDefinition>().Where(f => f.TenantId == tenantId && f.Category == category);
            if (!includeInactive)
            {
                query = query.Where(f => f.IsActive);
            }
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormulaDefinition>> GetByReturnTypeAsync(string returnType, string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormulaDefinition>().Where(f => f.TenantId == tenantId && f.ReturnType == returnType);
            if (!includeInactive)
            {
                query = query.Where(f => f.IsActive);
            }
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormulaDefinition>> GetByTagsAsync(IEnumerable<string> tags, string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var tagList = tags?.ToList() ?? new List<string>();
            var query = _context.Set<FormulaDefinition>().Where(f => f.TenantId == tenantId);
            if (tagList.Count > 0)
            {
                foreach (var tag in tagList)
                {
                    query = query.Where(f => f.Tags != null && f.Tags.Contains(tag));
                }
            }
            if (!includeInactive)
            {
                query = query.Where(f => f.IsActive);
            }
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<FormulaDefinition> Formulas, int TotalCount)> GetPagedAsync(
            string tenantId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? category = null,
            string? returnType = null,
            bool? isPublished = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormulaDefinition>().Where(f => f.TenantId == tenantId);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(f => f.Name.Contains(searchTerm) || (f.Description != null && f.Description.Contains(searchTerm)));
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(f => f.Category == category);
            }
            if (!string.IsNullOrWhiteSpace(returnType))
            {
                query = query.Where(f => f.ReturnType == returnType);
            }
            if (isPublished.HasValue)
            {
                query = query.Where(f => f.IsPublished == isPublished.Value);
            }
            if (!includeInactive)
            {
                query = query.Where(f => f.IsActive);
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(f => f.Name)
                .Skip((Math.Max(1, pageNumber) - 1) * Math.Max(1, pageSize))
                .Take(Math.Max(1, pageSize))
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        public async Task<IEnumerable<FormulaDefinition>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids?.ToList() ?? new List<Guid>();
            return await _context.Set<FormulaDefinition>()
                .Where(f => idList.Contains(f.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormulaDefinition>> GetDeprecatedAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormulaDefinition>()
                .Where(f => f.TenantId == tenantId && f.IsDeprecated)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormulaDefinition>> GetDependentFormulasAsync(Guid formulaId, string tenantId, CancellationToken cancellationToken = default)
        {
            var idString = formulaId.ToString();
            return await _context.Set<FormulaDefinition>()
                .Where(f => f.TenantId == tenantId && !string.IsNullOrEmpty(f.Dependencies) && f.Dependencies.Contains(idString))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormulaDefinition>()
                .Where(f => f.TenantId == tenantId && !string.IsNullOrEmpty(f.Category))
                .Select(f => f.Category)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<string>> GetTagsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var tagRows = await _context.Set<FormulaDefinition>()
                .Where(f => f.TenantId == tenantId && !string.IsNullOrEmpty(f.Tags))
                .Select(f => f.Tags)
                .ToListAsync(cancellationToken);

            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in tagRows)
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(row);
                    if (parsed != null)
                    {
                        foreach (var t in parsed)
                            all.Add(t);
                    }
                }
                catch
                {
                    // Fallback: if not JSON, split by commas
                    foreach (var t in row.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        all.Add(t);
                }
            }
            return all;
        }

        public async Task<int> GetCountAsync(string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormulaDefinition>().Where(f => f.TenantId == tenantId);
            if (!includeInactive)
            {
                query = query.Where(f => f.IsActive);
            }
            return await query.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormulaDefinition>> GetModifiedSinceAsync(string tenantId, DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormulaDefinition>()
                .Where(f => f.TenantId == tenantId && f.LastModifiedOn.HasValue && f.LastModifiedOn.Value >= since)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormulaDefinition>> SearchByExpressionAsync(string expressionPattern, string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormulaDefinition>()
                .Where(f => f.TenantId == tenantId && f.Expression.Contains(expressionPattern))
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetUsageStatisticsAsync(string tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            // Placeholder implementation; depends on evaluation logs aggregation
            var stats = await _context.Set<FormulaDefinition>()
                .Where(f => f.TenantId == tenantId)
                .Select(f => new { f.Id, Count = f.EvaluationLogs.Count })
                .ToListAsync(cancellationToken);

            return stats.ToDictionary(x => x.Id, x => x.Count);
        }

        public async Task AddAsync(FormulaDefinition formulaDefinition, CancellationToken cancellationToken = default)
        {
            await _context.Set<FormulaDefinition>().AddAsync(formulaDefinition, cancellationToken);
        }

        public Task UpdateAsync(FormulaDefinition formulaDefinition, CancellationToken cancellationToken = default)
        {
            _context.Set<FormulaDefinition>().Update(formulaDefinition);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(FormulaDefinition formulaDefinition, CancellationToken cancellationToken = default)
        {
            _context.Set<FormulaDefinition>().Remove(formulaDefinition);
            return Task.CompletedTask;
        }

        public async Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var set = _context.Set<FormulaDefinition>();
            var entity = set.Local.FirstOrDefault(f => f.Id == id) 
                         ?? await set.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

            if (entity != null)
            {
                set.Remove(entity);
            }
        }

        public async Task<bool> ExistsAsync(string name, string tenantId, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormulaDefinition>().Where(f => f.TenantId == tenantId && f.Name == name);
            if (excludeId.HasValue)
            {
                var id = excludeId.Value;
                query = query.Where(f => f.Id != id);
            }
            return await query.AnyAsync(cancellationToken);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}