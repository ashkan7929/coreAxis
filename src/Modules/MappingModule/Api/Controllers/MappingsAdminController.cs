using CoreAxis.Modules.MappingModule.Application.Commands;
using CoreAxis.Modules.MappingModule.Application.DTOs;
using CoreAxis.Modules.MappingModule.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreAxis.Modules.MappingModule.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class MappingsAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public MappingsAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("mappings")]
    public async Task<ActionResult<List<MappingDefinitionDto>>> GetMappings([FromQuery] string? tenantId)
    {
        var result = await _mediator.Send(new GetMappingDefinitionsQuery(tenantId));
        return Ok(result);
    }

    [HttpGet("mappings/{id}")]
    public async Task<ActionResult<MappingDefinitionDto>> GetMapping(Guid id)
    {
        var result = await _mediator.Send(new GetMappingDefinitionByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("mappings")]
    public async Task<ActionResult<Guid>> CreateMapping([FromBody] CreateMappingDefinitionDto dto)
    {
        var id = await _mediator.Send(new CreateMappingDefinitionCommand(
            dto.Name, 
            dto.SourceSchemaRef, 
            dto.TargetSchemaRef, 
            dto.RulesJson));
        
        return CreatedAtAction(nameof(GetMapping), new { id }, id);
    }

    [HttpPut("mappings/{id}")]
    public async Task<ActionResult> UpdateMapping(Guid id, [FromBody] UpdateMappingDefinitionDto dto)
    {
        var result = await _mediator.Send(new UpdateMappingDefinitionCommand(
            id, 
            dto.Name, 
            dto.SourceSchemaRef, 
            dto.TargetSchemaRef, 
            dto.RulesJson));
        
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("mappings/{id}/publish")]
    public async Task<ActionResult> PublishMapping(Guid id)
    {
        var result = await _mediator.Send(new PublishMappingDefinitionCommand(id));
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("mappings/{id}/test")]
    public async Task<ActionResult<TestMappingResponseDto>> TestMapping(Guid id, [FromBody] TestMappingRequestDto request)
    {
        var result = await _mediator.Send(new TestMappingDefinitionCommand(id, request.ContextJson));
        return Ok(result);
    }

    [HttpPost("mapping-sets")]
    public async Task<ActionResult<Guid>> CreateMappingSet([FromBody] CreateMappingSetDto dto)
    {
        var id = await _mediator.Send(new CreateMappingSetCommand(dto.Name, dto.ItemsJson));
        return CreatedAtAction(nameof(GetMappingSet), new { id }, id);
    }

    [HttpPut("mapping-sets/{id}")]
    public async Task<ActionResult> UpdateMappingSet(Guid id, [FromBody] UpdateMappingSetDto dto)
    {
        var result = await _mediator.Send(new UpdateMappingSetCommand(id, dto.Name, dto.ItemsJson));
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpGet("mapping-sets/{id}")]
    public async Task<ActionResult<MappingSetDto>> GetMappingSet(Guid id)
    {
        var result = await _mediator.Send(new GetMappingSetByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }
}
