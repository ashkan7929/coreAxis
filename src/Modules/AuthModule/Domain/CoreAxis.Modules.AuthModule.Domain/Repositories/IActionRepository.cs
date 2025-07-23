using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Repositories;

public interface IActionRepository : IRepository<CoreAxis.Modules.AuthModule.Domain.Entities.Action>
{
    Task<CoreAxis.Modules.AuthModule.Domain.Entities.Action?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsCodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<CoreAxis.Modules.AuthModule.Domain.Entities.Action>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}