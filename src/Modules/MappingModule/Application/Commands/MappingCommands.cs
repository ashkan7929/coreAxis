using CoreAxis.Modules.MappingModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.MappingModule.Application.Commands;

public record CreateMappingDefinitionCommand(string Name, string? SourceSchemaRef, string? TargetSchemaRef, string RulesJson) : IRequest<Guid>;

public record UpdateMappingDefinitionCommand(Guid Id, string? Name, string? SourceSchemaRef, string? TargetSchemaRef, string? RulesJson) : IRequest<bool>;

public record PublishMappingDefinitionCommand(Guid Id) : IRequest<bool>;

public record TestMappingDefinitionCommand(Guid Id, string ContextJson) : IRequest<TestMappingResponseDto>;

public record CreateMappingSetCommand(string Name, string ItemsJson) : IRequest<Guid>;

public record UpdateMappingSetCommand(Guid Id, string? Name, string? ItemsJson) : IRequest<bool>;

public record ExecuteMappingCommand(Guid MappingId, string ContextJson) : IRequest<TestMappingResponseDto>;
