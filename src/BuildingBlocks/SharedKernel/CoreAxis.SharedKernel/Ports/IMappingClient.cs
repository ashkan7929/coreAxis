using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.SharedKernel.Ports;

public interface IMappingClient
{
    Task<bool> MappingSetExistsAsync(Guid mappingSetId, CancellationToken cancellationToken = default);
    Task<bool> IsMappingSetPublishedAsync(Guid mappingSetId, CancellationToken cancellationToken = default);
}
