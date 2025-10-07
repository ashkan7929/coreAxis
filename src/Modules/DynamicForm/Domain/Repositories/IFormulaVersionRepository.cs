using System;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.DynamicForm.Domain.Repositories;

public interface IFormulaVersionRepository : IRepository<FormulaVersion>
{
    Task<FormulaVersion?> GetByFormulaDefinitionIdAndVersionAsync(Guid formulaDefinitionId, int versionNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<FormulaVersion>> GetByFormulaDefinitionIdAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default);
    Task<FormulaVersion?> GetActiveVersionAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default);
    Task<FormulaVersion?> GetLatestVersionAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default);
    Task<FormulaVersion?> GetLatestPublishedEffectiveVersionAsync(Guid formulaDefinitionId, DateTime asOfDate, CancellationToken cancellationToken = default);
    Task<int> GetNextVersionNumberAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveVersionAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default);
    Task DeactivateAllVersionsAsync(Guid formulaDefinitionId, CancellationToken cancellationToken = default);
    Task UpdateAsync(FormulaVersion formulaVersion, CancellationToken cancellationToken = default);
}