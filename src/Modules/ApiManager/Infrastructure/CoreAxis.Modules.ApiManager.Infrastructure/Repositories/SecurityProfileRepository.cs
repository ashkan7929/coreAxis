using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.Modules.ApiManager.Infrastructure;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Repositories;

public class SecurityProfileRepository : Repository<SecurityProfile>, ISecurityProfileRepository
{
    public SecurityProfileRepository(ApiManagerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SecurityProfile>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sp => sp.IsActive)
            .OrderBy(sp => sp.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(sp => sp.Name == name);
        
        if (excludeId.HasValue)
        {
            query = query.Where(sp => sp.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsUsedByWebServicesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WebService>()
            .AnyAsync(ws => ws.SecurityProfileId == id, cancellationToken);
    }
}
