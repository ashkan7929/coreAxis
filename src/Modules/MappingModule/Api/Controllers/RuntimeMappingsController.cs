using CoreAxis.Modules.MappingModule.Application.Commands;
using CoreAxis.Modules.MappingModule.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CoreAxis.Modules.MappingModule.Api.Controllers;

/// <summary>
/// Controller for executing mappings at runtime.
/// </summary>
[ApiController]
[Route("api/runtime/mappings")]
[Produces("application/json")]
public class RuntimeMappingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RuntimeMappingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Executes a mapping by ID with the provided context.
    /// </summary>
    /// <param name="request">The execution request.</param>
    /// <returns>The result of the mapping execution.</returns>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(TestMappingResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TestMappingResponseDto>> ExecuteMapping([FromBody] ExecuteMappingRequest request)
    {
        var result = await _mediator.Send(new ExecuteMappingCommand(request.MappingId, request.ContextJson));
        if (!result.Success && result.Error == "Mapping not found") return NotFound();
        if (!result.Success && result.Error == "Mapping is not published") return BadRequest("Mapping is not published");
        
        return Ok(result);
    }
}

/// <summary>
/// Request DTO for executing a mapping.
/// </summary>
public class ExecuteMappingRequest
{
    /// <summary>
    /// The ID of the mapping to execute.
    /// </summary>
    public Guid MappingId { get; set; }

    /// <summary>
    /// The JSON context for the mapping execution.
    /// </summary>
    public string ContextJson { get; set; } = "{}";
}