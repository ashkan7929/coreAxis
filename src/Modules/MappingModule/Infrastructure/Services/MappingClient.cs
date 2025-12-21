using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.MappingModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Ports;
using CoreAxis.SharedKernel.Versioning;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.MappingModule.Infrastructure.Services;

public class MappingClient : IMappingClient
{
    private readonly MappingDbContext _context;

    public MappingClient(MappingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> MappingSetExistsAsync(Guid mappingSetId, CancellationToken cancellationToken = default)
    {
        return await _context.MappingSets.AnyAsync(m => m.Id == mappingSetId, cancellationToken);
    }

    public async Task<bool> IsMappingSetPublishedAsync(Guid mappingSetId, CancellationToken cancellationToken = default)
    {
        // MappingSet currently doesn't track published status directly, assuming existence implies availability for now.
        // If MappingSet evolves to support versioning, this logic should be updated.
        // For now, check if it's active.
        return await _context.MappingSets.AnyAsync(m => m.Id == mappingSetId && m.IsActive, cancellationToken);
    }
}
