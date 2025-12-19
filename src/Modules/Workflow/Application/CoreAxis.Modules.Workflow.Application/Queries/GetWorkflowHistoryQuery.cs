using CoreAxis.Modules.Workflow.Application.DTOs;
using CoreAxis.Modules.Workflow.Infrastructure.Data;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.Workflow.Application.Queries;

public record GetWorkflowHistoryQuery(Guid RunId) : IRequest<Result<WorkflowRunHistoryDto>>;

public class GetWorkflowHistoryQueryHandler : IRequestHandler<GetWorkflowHistoryQuery, Result<WorkflowRunHistoryDto>>
{
    private readonly WorkflowDbContext _context;

    public GetWorkflowHistoryQueryHandler(WorkflowDbContext context)
    {
        _context = context;
    }

    public async Task<Result<WorkflowRunHistoryDto>> Handle(GetWorkflowHistoryQuery request, CancellationToken cancellationToken)
    {
        var runExists = await _context.WorkflowRuns.AnyAsync(r => r.Id == request.RunId, cancellationToken);
        if (!runExists)
            return Result<WorkflowRunHistoryDto>.Failure("Workflow run not found.");

        var steps = await _context.WorkflowRunSteps
            .Where(s => s.WorkflowRunId == request.RunId)
            .OrderBy(s => s.StartedAt)
            .Select(s => new WorkflowStepDto
            {
                StepId = s.StepId,
                StepType = s.StepType,
                Status = s.Status,
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                Error = s.Error
            })
            .ToListAsync(cancellationToken);

        var transitions = await _context.WorkflowTransitions
            .Where(t => t.WorkflowRunId == request.RunId)
            .OrderBy(t => t.EvaluatedAt)
            .Select(t => new WorkflowTransitionDto
            {
                FromStepId = t.FromStepId,
                ToStepId = t.ToStepId,
                EvaluatedAt = t.EvaluatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<WorkflowRunHistoryDto>.Success(new WorkflowRunHistoryDto
        {
            RunId = request.RunId,
            Steps = steps,
            Transitions = transitions
        });
    }
}
