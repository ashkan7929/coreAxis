using CoreAxis.Modules.MappingModule.Application.Commands;
using CoreAxis.Modules.MappingModule.Application.DTOs;
using CoreAxis.Modules.MappingModule.Application.Services;
using CoreAxis.Modules.MappingModule.Domain.Entities;
using CoreAxis.Modules.MappingModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Versioning;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.MappingModule.Application.Handlers;

/// <summary>
/// Handlers for mapping-related commands.
/// </summary>
public class MappingCommandHandlers : 
    IRequestHandler<CreateMappingDefinitionCommand, Guid>,
    IRequestHandler<UpdateMappingDefinitionCommand, bool>,
    IRequestHandler<PublishMappingDefinitionCommand, bool>,
    IRequestHandler<TestMappingDefinitionCommand, TestMappingResponseDto>,
    IRequestHandler<CreateMappingSetCommand, Guid>,
    IRequestHandler<UpdateMappingSetCommand, bool>,
    IRequestHandler<ExecuteMappingCommand, TestMappingResponseDto>
{
    private readonly MappingDbContext _context;
    private readonly ITransformEngine _transformEngine;
    private readonly Microsoft.Extensions.Logging.ILogger<MappingCommandHandlers> _logger;

    public MappingCommandHandlers(
        MappingDbContext context, 
        ITransformEngine transformEngine,
        Microsoft.Extensions.Logging.ILogger<MappingCommandHandlers> logger)
    {
        _context = context;
        _transformEngine = transformEngine;
        _logger = logger;
    }

    /// <summary>
    /// Handles the creation of a new mapping definition.
    /// </summary>
    public async Task<Guid> Handle(CreateMappingDefinitionCommand request, CancellationToken cancellationToken)
    {
        var mapping = new MappingDefinition
        {
            Name = request.Name,
            SourceSchemaRef = request.SourceSchemaRef,
            TargetSchemaRef = request.TargetSchemaRef,
            RulesJson = request.RulesJson,
            Status = VersionStatus.Draft,
            TenantId = "default" // TODO: Get from context
        };

        _context.MappingDefinitions.Add(mapping);
        await _context.SaveEntitiesAsync(cancellationToken);

        return mapping.Id;
    }

    /// <summary>
    /// Handles the update of an existing mapping definition.
    /// </summary>
    public async Task<bool> Handle(UpdateMappingDefinitionCommand request, CancellationToken cancellationToken)
    {
        var mapping = await _context.MappingDefinitions.FindAsync(new object[] { request.Id }, cancellationToken);
        if (mapping is null) return false;

        if (mapping.Status != VersionStatus.Draft)
        {
            throw new InvalidOperationException("Only draft mappings can be updated.");
        }

        if (request.Name != null) mapping.Name = request.Name;
        if (request.SourceSchemaRef != null) mapping.SourceSchemaRef = request.SourceSchemaRef;
        if (request.TargetSchemaRef != null) mapping.TargetSchemaRef = request.TargetSchemaRef;
        if (request.RulesJson != null) mapping.RulesJson = request.RulesJson;

        await _context.SaveEntitiesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Handles publishing a mapping definition.
    /// </summary>
    public async Task<bool> Handle(PublishMappingDefinitionCommand request, CancellationToken cancellationToken)
    {
        var mapping = await _context.MappingDefinitions.FindAsync(new object[] { request.Id }, cancellationToken);
        if (mapping is null) return false;

        if (mapping.Status == VersionStatus.Published) return true;

        // TODO: Validate rules before publishing?
        
        mapping.Status = VersionStatus.Published;
        mapping.PublishedAt = DateTime.UtcNow;

        await _context.SaveEntitiesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Handles testing a mapping definition.
    /// </summary>
    public async Task<TestMappingResponseDto> Handle(TestMappingDefinitionCommand request, CancellationToken cancellationToken)
    {
        var mapping = await _context.MappingDefinitions.FindAsync(new object[] { request.Id }, cancellationToken);
        if (mapping is null) 
            return new TestMappingResponseDto { Success = false, Error = "Mapping not found" };

        try
        {
            var output = await _transformEngine.ExecuteAsync(mapping.RulesJson, request.ContextJson, cancellationToken);
            return new TestMappingResponseDto { Success = true, OutputJson = output };
        }
        catch (Exception ex)
        {
            return new TestMappingResponseDto { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Handles creation of a mapping set.
    /// </summary>
    public async Task<Guid> Handle(CreateMappingSetCommand request, CancellationToken cancellationToken)
    {
        var mappingSet = new MappingSet
        {
            Name = request.Name,
            ItemsJson = request.ItemsJson,
            TenantId = "default" // TODO: Get from context
        };

        _context.MappingSets.Add(mappingSet);
        await _context.SaveEntitiesAsync(cancellationToken);

        return mappingSet.Id;
    }

    /// <summary>
    /// Handles update of a mapping set.
    /// </summary>
    public async Task<bool> Handle(UpdateMappingSetCommand request, CancellationToken cancellationToken)
    {
        var mappingSet = await _context.MappingSets.FindAsync(new object[] { request.Id }, cancellationToken);
        if (mappingSet is null) return false;

        if (request.Name != null) mappingSet.Name = request.Name;
        if (request.ItemsJson != null) mappingSet.ItemsJson = request.ItemsJson;

        await _context.SaveEntitiesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Handles execution of a mapping at runtime.
    /// </summary>
    public async Task<TestMappingResponseDto> Handle(ExecuteMappingCommand request, CancellationToken cancellationToken)
    {
        var mapping = await _context.MappingDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MappingId, cancellationToken);
            
        if (mapping is null) 
            return new TestMappingResponseDto { Success = false, Error = "Mapping not found" };

        if (mapping.Status != VersionStatus.Published)
            return new TestMappingResponseDto { Success = false, Error = "Mapping is not published" };

        try
        {
            var output = await _transformEngine.ExecuteAsync(mapping.RulesJson, request.ContextJson, cancellationToken);
            return new TestMappingResponseDto { Success = true, OutputJson = output };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing mapping {MappingId}", request.MappingId);
            return new TestMappingResponseDto { Success = false, Error = ex.Message };
        }
    }
}