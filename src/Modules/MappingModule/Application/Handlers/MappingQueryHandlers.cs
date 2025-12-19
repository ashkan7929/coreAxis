using CoreAxis.Modules.MappingModule.Application.DTOs;
using CoreAxis.Modules.MappingModule.Application.Queries;
using CoreAxis.Modules.MappingModule.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.MappingModule.Application.Handlers;

public class MappingQueryHandlers :
    IRequestHandler<GetMappingDefinitionByIdQuery, MappingDefinitionDto?>,
    IRequestHandler<GetMappingDefinitionsQuery, List<MappingDefinitionDto>>,
    IRequestHandler<GetMappingSetByIdQuery, MappingSetDto?>
{
    private readonly MappingDbContext _context;

    public MappingQueryHandlers(MappingDbContext context)
    {
        _context = context;
    }

    public async Task<MappingDefinitionDto?> Handle(GetMappingDefinitionByIdQuery request, CancellationToken cancellationToken)
    {
        var mapping = await _context.MappingDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (mapping == null) return null;

        return new MappingDefinitionDto
        {
            Id = mapping.Id,
            Name = mapping.Name,
            SourceSchemaRef = mapping.SourceSchemaRef,
            TargetSchemaRef = mapping.TargetSchemaRef,
            RulesJson = mapping.RulesJson,
            Status = mapping.Status,
            CreatedOn = mapping.CreatedOn,
            PublishedAt = mapping.PublishedAt
        };
    }

    public async Task<List<MappingDefinitionDto>> Handle(GetMappingDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MappingDefinitions.AsNoTracking();

        if (!string.IsNullOrEmpty(request.TenantId))
        {
            query = query.Where(m => m.TenantId == request.TenantId);
        }

        var mappings = await query.ToListAsync(cancellationToken);

        return mappings.Select(m => new MappingDefinitionDto
        {
            Id = m.Id,
            Name = m.Name,
            SourceSchemaRef = m.SourceSchemaRef,
            TargetSchemaRef = m.TargetSchemaRef,
            RulesJson = m.RulesJson,
            Status = m.Status,
            CreatedOn = m.CreatedOn,
            PublishedAt = m.PublishedAt
        }).ToList();
    }

    public async Task<MappingSetDto?> Handle(GetMappingSetByIdQuery request, CancellationToken cancellationToken)
    {
        var mappingSet = await _context.MappingSets.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (mappingSet == null) return null;

        return new MappingSetDto
        {
            Id = mappingSet.Id,
            Name = mappingSet.Name,
            ItemsJson = mappingSet.ItemsJson,
            CreatedOn = mappingSet.CreatedOn
        };
    }
}
