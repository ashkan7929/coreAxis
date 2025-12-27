using System.Collections.Generic;

namespace CoreAxis.SharedKernel.Ports;

public sealed record MappingExecutionResult(
    Dictionary<string, string> Headers,
    Dictionary<string, string> Query,
    string? BodyJson,
    Dictionary<string, object>? VarsPatch
);

public interface IMappingClient
{
    Task<bool> MappingSetExistsAsync(Guid mappingSetId, CancellationToken cancellationToken = default);
    Task<bool> IsMappingSetPublishedAsync(Guid mappingSetId, CancellationToken cancellationToken = default);
    Task<MappingExecutionResult> ExecuteMappingAsync(Guid mappingSetId, string inputJson, CancellationToken cancellationToken = default);
}
