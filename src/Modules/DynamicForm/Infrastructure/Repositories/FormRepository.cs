using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Repositories
{
    public class FormRepository : Repository<Form>, IFormRepository
    {
        public FormRepository(DynamicFormDbContext context) : base(context)
        {
        }

        public async Task<Form> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Form>().FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<Form> GetByIdWithIncludesAsync(Guid id, bool includeFields = false, bool includeSubmissions = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Form>().AsQueryable();

            if (includeFields)
            {
                query = query.Include(f => f.Fields);
            }

            if (includeSubmissions)
            {
                query = query.Include(f => f.Submissions);
            }

            return await query.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        public async Task<Form> GetByNameAsync(string name, string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Form>()
                .FirstOrDefaultAsync(f => f.Name == name && f.TenantId == tenantId, cancellationToken);
        }

        public async Task<Form> GetByNameWithIncludesAsync(string name, string tenantId, bool includeFields = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Form>().AsQueryable();

            if (includeFields)
            {
                query = query.Include(f => f.Fields);
            }

            return await query.FirstOrDefaultAsync(f => f.Name == name && f.TenantId == tenantId, cancellationToken);
        }

        public async Task<IEnumerable<Form>> GetByTenantAsync(string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Form>().Where(f => f.TenantId == tenantId);
            
            if (!includeInactive)
            {
                query = query.Where(f => f.IsActive);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Form>> GetPublishedByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Form>()
                .Where(f => f.TenantId == tenantId && f.IsPublished && f.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<Form> Forms, int TotalCount)> GetPagedAsync(
            string tenantId,
            int pageNumber,
            int pageSize,
            string searchTerm = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Form>().Where(f => f.TenantId == tenantId);

            if (!includeInactive)
            {
                query = query.Where(f => f.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(f => f.Name.Contains(searchTerm) || f.Description.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var forms = await query
                .OrderByDescending(f => f.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (forms, totalCount);
        }

        public async Task<IEnumerable<Form>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Form>()
                .Where(f => ids.Contains(f.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(string name, string tenantId, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Form>()
                .Where(f => f.Name == name && f.TenantId == tenantId);

            if (excludeId.HasValue)
            {
                query = query.Where(f => f.Id != excludeId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Form>().Where(f => f.TenantId == tenantId);

            if (!includeInactive)
            {
                query = query.Where(f => f.IsActive);
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<Form>> GetModifiedSinceAsync(string tenantId, DateTime since, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Form>()
                .Where(f => f.TenantId == tenantId && f.LastModifiedOn >= since)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Form form, CancellationToken cancellationToken = default)
        {
            await _context.Set<Form>().AddAsync(form, cancellationToken);
        }

        public async Task UpdateAsync(Form form, CancellationToken cancellationToken = default)
        {
            _context.Set<Form>().Update(form);
            await Task.CompletedTask;
        }

        public async Task RemoveAsync(Form form, CancellationToken cancellationToken = default)
        {
            _context.Set<Form>().Remove(form);
            await Task.CompletedTask;
        }

        public async Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var form = await GetByIdAsync(id, cancellationToken);
            if (form != null)
            {
                await RemoveAsync(form, cancellationToken);
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
