using CoreAxis.Modules.MappingModule.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace CoreAxis.Modules.MappingModule.Application.Queries;

/// <summary>
/// Query to get a mapping definition by its unique identifier.
/// </summary>
/// <param name="Id">The unique identifier of the mapping definition.</param>
public record GetMappingDefinitionByIdQuery(Guid Id) : IRequest<MappingDefinitionDto?>;

/// <summary>
/// Query to get a list of mapping definitions, optionally filtered by tenant.
/// </summary>
/// <param name="TenantId">Optional tenant ID to filter by.</param>
public record GetMappingDefinitionsQuery(string? TenantId = null) : IRequest<List<MappingDefinitionDto>>;

/// <summary>
/// Query to get a mapping set by its unique identifier.
/// </summary>
/// <param name="Id">The unique identifier of the mapping set.</param>
public record GetMappingSetByIdQuery(Guid Id) : IRequest<MappingSetDto?>;