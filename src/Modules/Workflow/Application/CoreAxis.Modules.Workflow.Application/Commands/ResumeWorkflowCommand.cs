using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.Workflow.Application.Commands;

public record ResumeWorkflowCommand(Guid WorkflowId, Dictionary<string, object> Input) : IRequest<Result<Guid>>;

public class ResumeWorkflowCommandHandler : IRequestHandler<ResumeWorkflowCommand, Result<Guid>>
{
    private readonly IWorkflowExecutor _executor;

    public ResumeWorkflowCommandHandler(IWorkflowExecutor executor)
    {
        _executor = executor;
    }

    public async Task<Result<Guid>> Handle(ResumeWorkflowCommand request, CancellationToken cancellationToken)
    {
        // TODO: Handle errors from ResumeAsync if it throws or returns result
        await _executor.ResumeAsync(request.WorkflowId, request.Input, cancellationToken);
        return Result<Guid>.Success(request.WorkflowId);
    }
}
