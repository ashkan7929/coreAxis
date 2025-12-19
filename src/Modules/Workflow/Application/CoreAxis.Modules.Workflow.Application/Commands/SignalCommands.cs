using CoreAxis.Modules.Workflow.Application.Services;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.Workflow.Application.Commands;

public record SignalWorkflowCommand(Guid WorkflowId, string SignalName, Dictionary<string, object> Payload) : IRequest<Result<Guid>>;

public class SignalWorkflowCommandHandler : IRequestHandler<SignalWorkflowCommand, Result<Guid>>
{
    private readonly IWorkflowExecutor _executor;

    public SignalWorkflowCommandHandler(IWorkflowExecutor executor)
    {
        _executor = executor;
    }

    public async Task<Result<Guid>> Handle(SignalWorkflowCommand request, CancellationToken cancellationToken)
    {
        await _executor.SignalAsync(request.WorkflowId, request.SignalName, request.Payload, cancellationToken);
        return Result<Guid>.Success(request.WorkflowId);
    }
}

public record CancelWorkflowCommand(Guid WorkflowId, string Reason) : IRequest<Result<Guid>>;

public class CancelWorkflowCommandHandler : IRequestHandler<CancelWorkflowCommand, Result<Guid>>
{
    private readonly IWorkflowExecutor _executor;

    public CancelWorkflowCommandHandler(IWorkflowExecutor executor)
    {
        _executor = executor;
    }

    public async Task<Result<Guid>> Handle(CancelWorkflowCommand request, CancellationToken cancellationToken)
    {
        await _executor.CancelAsync(request.WorkflowId, request.Reason, cancellationToken);
        return Result<Guid>.Success(request.WorkflowId);
    }
}
