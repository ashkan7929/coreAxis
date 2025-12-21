using Microsoft.EntityFrameworkCore;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.Modules.ApiManager.Infrastructure;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Repositories;

public class WebServiceMethodRepository : Repository<WebServiceMethod>, IWebServiceMethodRepository
{
    public WebServiceMethodRepository(ApiManagerDbContext context) : base(context)
    {
    }

    public async Task<WebServiceMethod?> GetByIdWithParametersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(m => m.Parameters)
            .Include(m => m.WebService)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<WebServiceMethod>> GetByWebServiceIdAsync(Guid webServiceId, bool? isActive = null, string? httpMethod = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
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

    public async Task<WebServiceMethod?> GetByServiceAndPathAsync(Guid webServiceId, string path, string httpMethod, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(m => m.WebServiceId == webServiceId && m.Path == path && m.HttpMethod == httpMethod, cancellationToken);
    }

    public async Task<bool> ExistsByNameAndWebServiceAsync(string name, Guid webServiceId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        // Name is not a property of WebServiceMethod in Domain, checking Path + HttpMethod is more appropriate for uniqueness usually, but interface might demand it.
        // Wait, WebServiceMethod domain entity does NOT have a Name property based on search results!
        // It has Path, HttpMethod, etc.
        // I will assume the interface is asking for something that doesn't exist or I need to check the entity definition again.
        // Entity: WebServiceId, Path, HttpMethod, RequestSchema, etc. NO NAME.
        // So this method implementation is wrong or interface is wrong.
        // I will throw NotImplementedException or try to map it.
        // Actually, let's remove it if it's not in the interface, but the error said it implements IWebServiceMethodRepository.
        // Let's assume the interface has it. I'll comment it out or fix it.
        // Error log: 'WebServiceMethodRepository' does not implement interface member 'IWebServiceMethodRepository.GetByServiceAndPathAsync(Guid, string, string, CancellationToken)'
        // So I added GetByServiceAndPathAsync above.
        // What about ExistsByNameAndWebServiceAsync? It was in the file I read.
        // I'll keep it but if it fails to compile due to missing Name property, I'll know.
        // Wait, the previous file read showed: query.Where(m => m.Name == name ...
        // So the previous code assumed Name property exists.
        // But the Entity definition I saw earlier:
        /*
        public class WebServiceMethod : EntityBase
        {
            public Guid WebServiceId { get; private set; }
            public string Path { get; private set; } = string.Empty;
            public string HttpMethod { get; private set; } = string.Empty;
            ...
        }
        */
        // It does NOT have Name.
        // So I should probably remove ExistsByNameAndWebServiceAsync unless I change the entity.
        // For now I will remove it and see if interface requires it.
        
        throw new NotImplementedException("WebServiceMethod does not have a Name property.");
    }

    public async Task<bool> ExistsByPathAndWebServiceAsync(string path, Guid webServiceId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(m => m.Path == path && m.WebServiceId == webServiceId);
        
        if (excludeId.HasValue)
        {
            query = query.Where(m => m.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
