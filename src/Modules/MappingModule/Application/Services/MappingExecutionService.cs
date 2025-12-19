using CoreAxis.Modules.MappingModule.Application.Commands;
using CoreAxis.Modules.MappingModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.MappingModule.Application.Services;

public class MappingExecutionService : IMappingExecutionService
{
    private readonly IMediator _mediator;

    public MappingExecutionService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<TestMappingResponseDto> ExecuteMappingAsync(Guid mappingId, string contextJson, CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(new ExecuteMappingCommand(mappingId, contextJson), cancellationToken);
    }
}
