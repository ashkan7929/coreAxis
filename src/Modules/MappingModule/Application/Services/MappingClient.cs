using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.MappingModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Ports;
using Microsoft.EntityFrameworkCore;
using MediatR;
using CoreAxis.Modules.MappingModule.Application.Commands;
using System.Text.Json;
using System.Collections.Generic;

namespace CoreAxis.Modules.MappingModule.Application.Services;

public class MappingClient : IMappingClient
{
    private readonly MappingDbContext _context;
    private readonly IMediator _mediator;

    public MappingClient(MappingDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<bool> MappingSetExistsAsync(Guid mappingSetId, CancellationToken cancellationToken = default)
    {
        return await _context.MappingSets.AnyAsync(m => m.Id == mappingSetId, cancellationToken);
    }

    public async Task<bool> IsMappingSetPublishedAsync(Guid mappingSetId, CancellationToken cancellationToken = default)
    {
        return await _context.MappingSets.AnyAsync(m => m.Id == mappingSetId && m.IsActive, cancellationToken);
    }

    public async Task<MappingExecutionResult> ExecuteMappingAsync(Guid mappingSetId, string inputJson, CancellationToken cancellationToken = default)
    {
        string outputJson = "{}";

        // Check if it's a MappingSet
        var set = await _context.MappingSets.FirstOrDefaultAsync(m => m.Id == mappingSetId, cancellationToken);
        if (set != null)
        {
            // Execute items in the set
            var items = JsonSerializer.Deserialize<List<string>>(set.ItemsJson);
            var currentJson = inputJson;
            
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (Guid.TryParse(item, out var defId))
                    {
                         var result = await _mediator.Send(new ExecuteMappingCommand(defId, currentJson), cancellationToken);
                         if (!result.Success) throw new Exception($"Mapping {defId} in Set {mappingSetId} failed: {result.Error}");
                         currentJson = result.OutputJson ?? currentJson;
                    }
                }
            }
            outputJson = currentJson;
        }
        else
        {
            // Fallback: Check if it's a direct MappingDefinition
            var def = await _context.MappingDefinitions.AnyAsync(m => m.Id == mappingSetId, cancellationToken);
            if (def)
            {
                var result = await _mediator.Send(new ExecuteMappingCommand(mappingSetId, inputJson), cancellationToken);
                if (!result.Success) throw new Exception($"Mapping {mappingSetId} failed: {result.Error}");
                outputJson = result.OutputJson ?? inputJson;
            }
            else
            {
                throw new Exception($"MappingSet or Definition {mappingSetId} not found");
            }
        }

        // Parse outputJson into MappingExecutionResult
        // Assuming outputJson matches the structure: { "headers": {}, "query": {}, "body": {}, "varsPatch": {} }
        // If not, we try to be smart.
        
        try 
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var rawResult = JsonSerializer.Deserialize<RawMappingResult>(outputJson, options);
            
            if (rawResult != null)
            {
                // Logic refinement:
                // 1. If rawResult.Body is null, BodyJson MUST be null (no empty object)
                // 2. Fallback: If the JSON doesn't match the RawMappingResult structure (i.e. all properties null),
                //    it might be a simple object intended for VarsPatch.
                
                bool isStructured = rawResult.Headers != null || rawResult.Query != null || rawResult.Body != null || rawResult.VarsPatch != null;
                
                if (isStructured)
                {
                    string? bodyJson = null;
                    if (rawResult.Body != null)
                    {
                        // Check if Body is explicitly null or empty
                        // rawResult.Body is object?, so if it's not null, serialize it.
                        // However, user said "if rawResult.Body null -> null". 
                        // If it IS null, bodyJson remains null.
                        bodyJson = JsonSerializer.Serialize(rawResult.Body);
                    }

                    return new MappingExecutionResult(
                        rawResult.Headers ?? new(),
                        rawResult.Query ?? new(),
                        bodyJson,
                        rawResult.VarsPatch ?? new()
                    );
                }
            }
        }
        catch
        {
            // If parsing fails, fall through to fallback
        }

        // Fallback: When outputJson is a simple object (and parsing as structured failed or returned all nulls)
        // User requested: Only populate VarsPatch, keep BodyJson null to avoid ambiguity.
        var vars = JsonSerializer.Deserialize<Dictionary<string, object>>(outputJson) ?? new();
        return new MappingExecutionResult(new(), new(), null, vars);
    }
    
    private class RawMappingResult
    {
        public Dictionary<string, string>? Headers { get; set; }
        public Dictionary<string, string>? Query { get; set; }
        public object? Body { get; set; }
        public Dictionary<string, object>? VarsPatch { get; set; }
    }
}
