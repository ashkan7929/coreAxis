using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.SharedKernel.Ports;

public interface IFormulaClient
{
    Task<bool> FormulaExistsAsync(Guid formulaId, string? version = null, CancellationToken cancellationToken = default);
    Task<bool> IsFormulaPublishedAsync(Guid formulaId, string version, CancellationToken cancellationToken = default);
    Task<Result<Dictionary<string, object>>> EvaluateAsync(Guid formulaId, string version, Dictionary<string, object> inputs, CancellationToken cancellationToken = default);
}
