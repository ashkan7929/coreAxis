using CoreAxis.Modules.Workflow.Application.DTOs;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.Workflow.Application.Queries;

public record GetWorkflowRunQuery(Guid RunId) : IRequest<Result<WorkflowRunDto>>;

public class GetWorkflowRunQueryHandler : IRequestHandler<GetWorkflowRunQuery, Result<WorkflowRunDto>>
{
    private readonly WorkflowDbContext _context;

    public GetWorkflowRunQueryHandler(WorkflowDbContext context)
    {
        _context = context;
    }

    public async Task<Result<WorkflowRunDto>> Handle(GetWorkflowRunQuery request, CancellationToken cancellationToken)
    {
        var run = await _context.WorkflowRuns
            .FirstOrDefaultAsync(r => r.Id == request.RunId, cancellationToken);

        if (run == null)
            return Result<WorkflowRunDto>.Failure("Workflow run not found.");

        return Result<WorkflowRunDto>.Success(new WorkflowRunDto
        {
            Id = run.Id,
            DefinitionCode = run.WorkflowDefinitionCode,
            VersionNumber = run.VersionNumber,
            Status = run.Status,
            ContextJson = run.ContextJson,
            CorrelationId = run.CorrelationId,
            CreatedAt = run.CreatedOn,
            UpdatedAt = run.LastModifiedOn
        });
    }
}
