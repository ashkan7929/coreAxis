using CoreAxis.Modules.MappingModule.Application.Commands;
using CoreAxis.Modules.MappingModule.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.MappingModule.Api.Controllers;

[ApiController]
[Route("api/runtime/mappings")]
public class RuntimeMappingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RuntimeMappingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("execute")]
    public async Task<ActionResult<TestMappingResponseDto>> ExecuteMapping([FromBody] ExecuteMappingRequest request)
    {
        var result = await _mediator.Send(new ExecuteMappingCommand(request.MappingId, request.ContextJson));
        if (!result.Success && result.Error == "Mapping not found") return NotFound();
        if (!result.Success && result.Error == "Mapping is not published") return BadRequest("Mapping is not published");
        
        return Ok(result);
    }
}

public class ExecuteMappingRequest
{
    public Guid MappingId { get; set; }
    public string ContextJson { get; set; } = "{}";
}
