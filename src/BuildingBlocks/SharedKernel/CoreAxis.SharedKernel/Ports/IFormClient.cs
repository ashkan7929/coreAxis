using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.SharedKernel.Ports;

public interface IFormClient
{
    Task<bool> FormExistsAsync(Guid formId, string? version = null, CancellationToken cancellationToken = default);
    Task<bool> IsFormPublishedAsync(Guid formId, string version, CancellationToken cancellationToken = default);
}
