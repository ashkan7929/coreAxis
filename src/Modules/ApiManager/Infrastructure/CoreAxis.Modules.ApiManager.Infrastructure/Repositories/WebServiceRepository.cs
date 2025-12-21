using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.Modules.ApiManager.Infrastructure;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Repositories;

public class WebServiceRepository : Repository<WebService>, IWebServiceRepository
{
    public WebServiceRepository(ApiManagerDbContext context) : base(context)
    {
    }

    public async Task<WebService?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ws => ws.SecurityProfile)
            .Include(ws => ws.Methods.Where(m => m.IsActive))
                .ThenInclude(m => m.Parameters)
            .FirstOrDefaultAsync(ws => ws.Id == id, cancellationToken);
    }

    public async Task<WebService?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(ws => ws.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<WebService>> GetAllWithSecurityProfileAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(ws => ws.SecurityProfile).AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(ws => ws.IsActive == isActive.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(ws => ws.Name == name);
        
        if (excludeId.HasValue)
        {
            query = query.Where(ws => ws.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByBaseUrlAsync(string baseUrl, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(ws => ws.BaseUrl == baseUrl);
        
        if (excludeId.HasValue)
        {
            query = query.Where(ws => ws.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
