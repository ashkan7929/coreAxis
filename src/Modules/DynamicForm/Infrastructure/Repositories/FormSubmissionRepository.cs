using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Repositories
{
    public class FormSubmissionRepository : Repository<FormSubmission>, IFormSubmissionRepository
    {
        public FormSubmissionRepository(DynamicFormDbContext context) : base(context)
        {
        }

        public new async Task<FormSubmission> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormSubmission>().FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<FormSubmission> GetByIdWithFormAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormSubmission>()
                .Include(s => s.Form)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<FormSubmission>> GetByFormIdAsync(Guid formId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormSubmission>().Where(s => s.FormId == formId);
            
            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.OrderByDescending(s => s.CreatedOn).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormSubmission>> GetByUserIdAsync(Guid userId, string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormSubmission>()
                .Where(s => s.UserId == userId && s.TenantId == tenantId);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.OrderByDescending(s => s.CreatedOn).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormSubmission>> GetByTenantAsync(string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormSubmission>().Where(s => s.TenantId == tenantId);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.OrderByDescending(s => s.CreatedOn).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormSubmission>> GetByStatusAsync(string status, string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormSubmission>()
                .Where(s => s.Status == status && s.TenantId == tenantId && s.IsActive)
                .OrderByDescending(s => s.CreatedOn)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<FormSubmission> Submissions, int TotalCount)> GetPagedAsync(
            string tenantId,
            int pageNumber,
            int pageSize,
            Guid? formId = null,
            string status = null,
            Guid? userId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            bool includeInactive = false,
            bool includeForm = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormSubmission>().AsQueryable();

            if (includeForm)
            {
                query = query.Include(s => s.Form);
            }

            if (!string.IsNullOrEmpty(tenantId))
            {
                query = query.Where(s => s.TenantId == tenantId);
            }

            if (formId.HasValue)
            {
                query = query.Where(s => s.FormId == formId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            if (userId.HasValue)
            {
                query = query.Where(s => s.UserId == userId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(s => s.CreatedOn >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.CreatedOn <= toDate.Value);
            }

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(s => s.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<IEnumerable<FormSubmission>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormSubmission>()
                .Where(s => ids.Contains(s.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormSubmission>> GetPendingSubmissionsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormSubmission>()
                .Where(s => s.TenantId == tenantId && (s.Status == "Draft" || s.Status == "Submitted") && s.IsActive)
                .OrderByDescending(s => s.CreatedOn)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormSubmission>> GetSubmissionsAwaitingApprovalAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormSubmission>()
                .Where(s => s.TenantId == tenantId && s.Status == "PendingApproval" && s.IsActive)
                .OrderByDescending(s => s.CreatedOn)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormSubmission>> GetSubmissionsWithErrorsAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormSubmission>()
                .Where(s => s.TenantId == tenantId && !string.IsNullOrEmpty(s.ValidationErrors) && s.IsActive)
                .OrderByDescending(s => s.CreatedOn)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormSubmission>> GetByDateRangeAsync(string tenantId, DateTime fromDate, DateTime toDate, Guid? formId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormSubmission>()
                .Where(s => s.TenantId == tenantId && s.CreatedOn >= fromDate && s.CreatedOn <= toDate && s.IsActive);

            if (formId.HasValue)
            {
                query = query.Where(s => s.FormId == formId.Value);
            }

            return await query.OrderByDescending(s => s.CreatedOn).ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(Guid formId, string status = null, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormSubmission>().Where(s => s.FormId == formId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task<int> GetCountByTenantAsync(string tenantId, string status = null, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormSubmission>().Where(s => s.TenantId == tenantId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<FormSubmission>> GetModifiedSinceAsync(string tenantId, DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FormSubmission>()
                .Where(s => s.TenantId == tenantId && s.LastModifiedOn >= since)
                .OrderByDescending(s => s.LastModifiedOn)
                .ToListAsync(cancellationToken);
        }

        public async Task<Dictionary<string, int>> GetSubmissionStatisticsAsync(Guid formId, CancellationToken cancellationToken = default)
        {
            var stats = await _context.Set<FormSubmission>()
                .Where(s => s.FormId == formId && s.IsActive)
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

            return stats;
        }

        public async Task<Dictionary<string, int>> GetSubmissionStatisticsByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var stats = await _context.Set<FormSubmission>()
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

            return stats;
        }

        public async Task<SubmissionStats> GetStatsAsync(Guid formId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<FormSubmission>().Where(s => s.FormId == formId && s.IsActive);

            if (fromDate.HasValue)
            {
                query = query.Where(s => s.CreatedOn >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.CreatedOn <= toDate.Value);
            }

            var submissions = await query.Select(s => new { s.CreatedOn, s.Status }).ToListAsync(cancellationToken);

            var stats = new SubmissionStats
            {
                FormId = formId,
                TotalSubmissions = submissions.Count,
                FirstSubmissionDate = submissions.Any() ? submissions.Min(s => s.CreatedOn) : null,
                LastSubmissionDate = submissions.Any() ? submissions.Max(s => s.CreatedOn) : null
            };

            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            stats.SubmissionsToday = submissions.Count(s => s.CreatedOn.Date == today);
            stats.SubmissionsThisWeek = submissions.Count(s => s.CreatedOn.Date >= startOfWeek);
            stats.SubmissionsThisMonth = submissions.Count(s => s.CreatedOn.Date >= startOfMonth);

            stats.SubmissionsByStatus = submissions
                .GroupBy(s => s.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }

        public async Task AddAsync(FormSubmission entity, CancellationToken cancellationToken = default)
        {
            await _context.Set<FormSubmission>().AddAsync(entity, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<FormSubmission> formSubmissions, CancellationToken cancellationToken = default)
        {
            await _context.Set<FormSubmission>().AddRangeAsync(formSubmissions, cancellationToken);
        }

        public async Task UpdateAsync(FormSubmission entity, CancellationToken cancellationToken = default)
        {
            _context.Set<FormSubmission>().Update(entity);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsync(IEnumerable<FormSubmission> formSubmissions, CancellationToken cancellationToken = default)
        {
            _context.Set<FormSubmission>().UpdateRange(formSubmissions);
            await Task.CompletedTask;
        }

        public async Task RemoveAsync(FormSubmission formSubmission, CancellationToken cancellationToken = default)
        {
            _context.Set<FormSubmission>().Remove(formSubmission);
            await Task.CompletedTask;
        }

        public async Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Set<FormSubmission>().FindAsync(new object[] { id }, cancellationToken);
            if (entity != null)
            {
                _context.Set<FormSubmission>().Remove(entity);
            }
        }

        public async Task RemoveByFormIdAsync(Guid formId, CancellationToken cancellationToken = default)
        {
            var entities = await _context.Set<FormSubmission>().Where(s => s.FormId == formId).ToListAsync(cancellationToken);
            if (entities.Any())
            {
                _context.Set<FormSubmission>().RemoveRange(entities);
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
