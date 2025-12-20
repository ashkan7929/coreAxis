using System;
using CoreAxis.Modules.MappingModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.MappingModule.Application.Commands;

/// <summary>
/// Command to create a new mapping definition.
/// </summary>
/// <param name="Name">Name of the mapping.</param>
/// <param name="SourceSchemaRef">Reference to the source schema.</param>
/// <param name="TargetSchemaRef">Reference to the target schema.</param>
/// <param name="RulesJson">JSON string containing mapping rules.</param>
public record CreateMappingDefinitionCommand(string Name, string? SourceSchemaRef, string? TargetSchemaRef, string RulesJson) : IRequest<Guid>;

/// <summary>
/// Command to update an existing mapping definition.
/// </summary>
/// <param name="Id">The unique identifier of the mapping.</param>
/// <param name="Name">Name of the mapping.</param>
/// <param name="SourceSchemaRef">Reference to the source schema.</param>
/// <param name="TargetSchemaRef">Reference to the target schema.</param>
/// <param name="RulesJson">JSON string containing mapping rules.</param>
public record UpdateMappingDefinitionCommand(Guid Id, string? Name, string? SourceSchemaRef, string? TargetSchemaRef, string? RulesJson) : IRequest<bool>;

/// <summary>
/// Command to publish a mapping definition.
/// </summary>
/// <param name="Id">The unique identifier of the mapping.</param>
public record PublishMappingDefinitionCommand(Guid Id) : IRequest<bool>;

/// <summary>
/// Command to test a mapping definition without persisting changes.
/// </summary>
/// <param name="Id">The unique identifier of the mapping.</param>
/// <param name="ContextJson">The context JSON for testing.</param>
public record TestMappingDefinitionCommand(Guid Id, string ContextJson) : IRequest<TestMappingResponseDto>;

/// <summary>
/// Command to create a new mapping set.
/// </summary>
/// <param name="Name">Name of the mapping set.</param>
/// <param name="ItemsJson">JSON string containing items.</param>
public record CreateMappingSetCommand(string Name, string ItemsJson) : IRequest<Guid>;

/// <summary>
/// Command to update an existing mapping set.
/// </summary>
/// <param name="Id">The unique identifier of the mapping set.</param>
/// <param name="Name">Name of the mapping set.</param>
/// <param name="ItemsJson">JSON string containing items.</param>
public record UpdateMappingSetCommand(Guid Id, string? Name, string? ItemsJson) : IRequest<bool>;

/// <summary>
/// Command to execute a mapping definition at runtime.
/// </summary>
/// <param name="MappingId">The unique identifier of the mapping.</param>
/// <param name="ContextJson">The context JSON for execution.</param>
public record ExecuteMappingCommand(Guid MappingId, string ContextJson) : IRequest<TestMappingResponseDto>;