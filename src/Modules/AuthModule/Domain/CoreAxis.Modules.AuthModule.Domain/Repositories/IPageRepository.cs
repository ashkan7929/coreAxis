using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Repositories;

public interface IPageRepository : IRepository<Page>
{
    Task<Page?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsCodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<Page>> GetByModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<IEnumerable<Page>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}