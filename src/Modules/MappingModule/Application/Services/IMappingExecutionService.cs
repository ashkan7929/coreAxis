using CoreAxis.Modules.MappingModule.Application.DTOs;

namespace CoreAxis.Modules.MappingModule.Application.Services;

public interface IMappingExecutionService
{
    Task<TestMappingResponseDto> ExecuteMappingAsync(Guid mappingId, string contextJson, CancellationToken cancellationToken = default);
}
