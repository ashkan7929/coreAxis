using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.Shared.Infrastructure.Repositories;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Repositories;

public class WebServiceMethodRepository : Repository<WebServiceMethod>, IWebServiceMethodRepository
{
    public WebServiceMethodRepository(ApiManagerDbContext context) : base(context)
    {
    }

    public async Task<WebServiceMethod?> GetByIdWithParametersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(m => m.Parameters)
            .Include(m => m.WebService)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<WebServiceMethod>> GetByWebServiceIdAsync(Guid webServiceId, bool? isActive = null, string? httpMethod = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(m => m.Parameters)
            .Where(m => m.WebServiceId == webServiceId);

        if (isActive.HasValue)
        {
            query = query.Where(m => m.IsActive == isActive.Value);
        }

        if (!string.IsNullOrEmpty(httpMethod))
        {
            query = query.Where(m => m.HttpMethod == httpMethod);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAndWebServiceAsync(string name, Guid webServiceId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(m => m.Name == name && m.WebServiceId == webServiceId);
        
        if (excludeId.HasValue)
        {
            query = query.Where(m => m.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByPathAndWebServiceAsync(string path, Guid webServiceId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(m => m.Path == path && m.WebServiceId == webServiceId);
        
        if (excludeId.HasValue)
        {
            query = query.Where(m => m.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}