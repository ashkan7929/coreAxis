using CoreAxis.Modules.MappingModule.Application.Commands;
using CoreAxis.Modules.MappingModule.Application.DTOs;
using CoreAxis.Modules.MappingModule.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreAxis.Modules.MappingModule.Api.Controllers;

/// <summary>
/// Controller for managing mapping definitions and sets.
/// </summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
public class MappingsAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public MappingsAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets a list of mapping definitions.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID to filter by.</param>
    /// <returns>A list of mapping definitions.</returns>
    [HttpGet("mappings")]
    [ProducesResponseType(typeof(List<MappingDefinitionDto>), 200)]
    public async Task<ActionResult<List<MappingDefinitionDto>>> GetMappings([FromQuery] string? tenantId)
    {
        var result = await _mediator.Send(new GetMappingDefinitionsQuery(tenantId));
        return Ok(result);
    }

    /// <summary>
    /// Gets a mapping definition by ID.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <returns>The mapping definition.</returns>
    [HttpGet("mappings/{id}")]
    [ProducesResponseType(typeof(MappingDefinitionDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<MappingDefinitionDto>> GetMapping(Guid id)
    {
        var result = await _mediator.Send(new GetMappingDefinitionByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Creates a new mapping definition.
    /// </summary>
    /// <param name="dto">The creation data.</param>
    /// <returns>The ID of the created mapping.</returns>
    [HttpPost("mappings")]
    [ProducesResponseType(typeof(Guid), 201)]
    public async Task<ActionResult<Guid>> CreateMapping([FromBody] CreateMappingDefinitionDto dto)
    {
        var id = await _mediator.Send(new CreateMappingDefinitionCommand(
            dto.Name, 
            dto.SourceSchemaRef, 
            dto.TargetSchemaRef, 
            dto.RulesJson));
        
        return CreatedAtAction(nameof(GetMapping), new { id }, id);
    }

    /// <summary>
    /// Updates an existing mapping definition.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <param name="dto">The update data.</param>
    /// <returns>No content if successful.</returns>
    [HttpPut("mappings/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
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

    /// <summary>
    /// Publishes a mapping definition.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("mappings/{id}/publish")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> PublishMapping(Guid id)
    {
        var result = await _mediator.Send(new PublishMappingDefinitionCommand(id));
        if (!result) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Tests a mapping definition with provided context.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <param name="request">The test request containing context JSON.</param>
    /// <returns>The transformation result.</returns>
    [HttpPost("mappings/{id}/test")]
    [ProducesResponseType(typeof(TestMappingResponseDto), 200)]
    public async Task<ActionResult<TestMappingResponseDto>> TestMapping(Guid id, [FromBody] TestMappingRequestDto request)
    {
        var result = await _mediator.Send(new TestMappingDefinitionCommand(id, request.ContextJson));
        return Ok(result);
    }

    /// <summary>
    /// Creates a new mapping set.
    /// </summary>
    /// <param name="dto">The creation data.</param>
    /// <returns>The ID of the created mapping set.</returns>
    [HttpPost("mapping-sets")]
    [ProducesResponseType(typeof(Guid), 201)]
    public async Task<ActionResult<Guid>> CreateMappingSet([FromBody] CreateMappingSetDto dto)
    {
        var id = await _mediator.Send(new CreateMappingSetCommand(dto.Name, dto.ItemsJson));
        return CreatedAtAction(nameof(GetMappingSet), new { id }, id);
    }

    /// <summary>
    /// Updates an existing mapping set.
    /// </summary>
    /// <param name="id">The mapping set ID.</param>
    /// <param name="dto">The update data.</param>
    /// <returns>No content if successful.</returns>
    [HttpPut("mapping-sets/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> UpdateMappingSet(Guid id, [FromBody] UpdateMappingSetDto dto)
    {
        var result = await _mediator.Send(new UpdateMappingSetCommand(id, dto.Name, dto.ItemsJson));
        if (!result) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Gets a mapping set by ID.
    /// </summary>
    /// <param name="id">The mapping set ID.</param>
    /// <returns>The mapping set.</returns>
    [HttpGet("mapping-sets/{id}")]
    [ProducesResponseType(typeof(MappingSetDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<MappingSetDto>> GetMappingSet(Guid id)
    {
        var result = await _mediator.Send(new GetMappingSetByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }
}