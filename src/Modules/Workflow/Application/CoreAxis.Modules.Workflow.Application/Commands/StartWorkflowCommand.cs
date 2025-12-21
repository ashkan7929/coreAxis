using CoreAxis.Modules.Workflow.Application.DTOs;
using CoreAxis.Modules.Workflow.Domain.Entities;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Versioning;
using MediatR;
using Microsoft.EntityFrameworkCore;

using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.Commands;

public record StartWorkflowCommand(StartWorkflowDto Dto) : IRequest<Result<Guid>>;

public class StartWorkflowCommandHandler : IRequestHandler<StartWorkflowCommand, Result<Guid>>
{
    private readonly WorkflowDbContext _context;

    public StartWorkflowCommandHandler(WorkflowDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(StartWorkflowCommand request, CancellationToken cancellationToken)
    {
        // 1. Find Definition
        var definition = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(d => d.Code == request.Dto.DefinitionCode, cancellationToken);
        
        if (definition == null)
            return Result<Guid>.Failure($"Workflow definition '{request.Dto.DefinitionCode}' not found.");

        // 2. Find Version
        WorkflowDefinitionVersion? version;
        if (request.Dto.VersionNumber.HasValue)
        {
            version = await _context.WorkflowDefinitionVersions
                .FirstOrDefaultAsync(v => v.WorkflowDefinitionId == definition.Id && v.VersionNumber == request.Dto.VersionNumber, cancellationToken);
        }
        else
        {
            // Get latest published
            version = await _context.WorkflowDefinitionVersions
                .Where(v => v.WorkflowDefinitionId == definition.Id && v.Status == VersionStatus.Published)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (version == null)
            return Result<Guid>.Failure($"Workflow version not found for '{request.Dto.DefinitionCode}'.");

        // Enforce Published status unless debug flag is present
        if (version.Status != VersionStatus.Published)
        {
            bool isDebug = false;
            try 
            {
                using var doc = JsonDocument.Parse(request.Dto.ContextJson);
                if (doc.RootElement.TryGetProperty("debug", out var debugProp) && debugProp.ValueKind == JsonValueKind.True)
                {
                    isDebug = true;
                }
            } 
            catch {}

            if (!isDebug)
            {
                return Result<Guid>.Failure($"Cannot start workflow version {version.VersionNumber} because it is not Published. (Set 'debug': true in context to override)");
            }
        }

        // 3. Create Run
        var run = new WorkflowRun
        {
            WorkflowDefinitionCode = definition.Code,
            VersionNumber = version.VersionNumber,
            Status = "Running", // Use enum later
            ContextJson = request.Dto.ContextJson,
            CorrelationId = request.Dto.CorrelationId ?? Guid.NewGuid().ToString(),
            CreatedOn = DateTime.UtcNow
        };

        // 4. Start (Logic to start first step would go here or be triggered by domain event)
        // For now just persist the run
        run.Start();

        await _context.WorkflowRuns.AddAsync(run, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(run.Id);
    }
}
