using CoreAxis.Modules.MappingModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.MappingModule.Application.Queries;

public record GetMappingDefinitionByIdQuery(Guid Id) : IRequest<MappingDefinitionDto?>;

public record GetMappingDefinitionsQuery(string? TenantId = null) : IRequest<List<MappingDefinitionDto>>;

public record GetMappingSetByIdQuery(Guid Id) : IRequest<MappingSetDto?>;
