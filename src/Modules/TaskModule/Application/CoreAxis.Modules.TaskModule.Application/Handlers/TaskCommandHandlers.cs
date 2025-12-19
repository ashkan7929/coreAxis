using CoreAxis.EventBus;
using CoreAxis.Modules.TaskModule.Application.Commands;
using CoreAxis.Modules.TaskModule.Domain.Entities;
using CoreAxis.Modules.TaskModule.Infrastructure.Data;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Contracts.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CoreAxis.Modules.TaskModule.Application.Handlers;

public class TaskCommandHandlers :
    IRequestHandler<ClaimTaskCommand, Result<bool>>,
    IRequestHandler<ApproveTaskCommand, Result<bool>>,
    IRequestHandler<RejectTaskCommand, Result<bool>>,
    IRequestHandler<ReturnTaskCommand, Result<bool>>,
    IRequestHandler<DelegateTaskCommand, Result<bool>>
{
    private readonly TaskDbContext _context;
    private readonly IEventBus _eventBus;

    public TaskCommandHandlers(TaskDbContext context, IEventBus eventBus)
    {
        _context = context;
        _eventBus = eventBus;
    }

    public async Task<Result<bool>> Handle(ClaimTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskInstances.FindAsync(new object[] { request.TaskId }, cancellationToken);
        if (task == null) return Result<bool>.Failure("Task not found");

        if (task.Status != "Open") return Result<bool>.Failure("Task is not open");

        // Allow claim if assigned to a Role (we assume the caller has checked role membership in Controller/Policy)
        // Or strictly check here if we pass roles in command.
        // For MVP, we assume the user has permission to see it (via GetInbox) so they can claim it if it's Role-based.
        if (task.AssigneeType != "Role") return Result<bool>.Failure("Can only claim tasks assigned to a role");

        task.AssigneeType = "User";
        task.AssigneeId = request.UserId;
        task.Status = "Assigned";
        
        _context.TaskActionLogs.Add(new TaskActionLog
        {
            TaskId = task.Id,
            Action = "Claim",
            ActorId = request.UserId
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(ApproveTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskInstances.FindAsync(new object[] { request.TaskId }, cancellationToken);
        if (task == null) return Result<bool>.Failure("Task not found");

        if (task.AssigneeType != "User" || task.AssigneeId != request.UserId)
            return Result<bool>.Failure("Not assigned to you");

        task.Status = "Completed";
        task.CompletedAt = DateTime.UtcNow;

        _context.TaskActionLogs.Add(new TaskActionLog
        {
            TaskId = task.Id,
            Action = "Approve",
            ActorId = request.UserId,
            Comment = request.Comment,
            PayloadJson = request.Payload != null ? JsonSerializer.Serialize(request.Payload) : null
        });

        await _context.SaveChangesAsync(cancellationToken);

        // Publish task completed event
        await _eventBus.PublishAsync(new HumanTaskCompleted(
            task.WorkflowId,
            task.Id,
            "Approved",
            request.Payload != null ? JsonSerializer.Serialize(request.Payload) : null,
            request.Comment,
            Guid.NewGuid() // CorrelationId
        ));

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(RejectTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskInstances.FindAsync(new object[] { request.TaskId }, cancellationToken);
        if (task == null) return Result<bool>.Failure("Task not found");

        if (task.AssigneeType != "User" || task.AssigneeId != request.UserId)
            return Result<bool>.Failure("Not assigned to you");

        task.Status = "Rejected"; 
        task.CompletedAt = DateTime.UtcNow;

        _context.TaskActionLogs.Add(new TaskActionLog
        {
            TaskId = task.Id,
            Action = "Reject",
            ActorId = request.UserId,
            Comment = request.Comment
        });

        await _context.SaveChangesAsync(cancellationToken);

        // Publish task completed event
        await _eventBus.PublishAsync(new HumanTaskCompleted(
            task.WorkflowId,
            task.Id,
            "Rejected",
            null,
            request.Comment,
            Guid.NewGuid() // CorrelationId
        ));

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(ReturnTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskInstances.FindAsync(new object[] { request.TaskId }, cancellationToken);
        if (task == null) return Result<bool>.Failure("Task not found");

        if (task.AssigneeType != "User" || task.AssigneeId != request.UserId)
            return Result<bool>.Failure("Not assigned to you");

        task.Status = "Returned";
        task.CompletedAt = DateTime.UtcNow;

        _context.TaskActionLogs.Add(new TaskActionLog
        {
            TaskId = task.Id,
            Action = "Return",
            ActorId = request.UserId,
            Comment = request.Comment
        });

        await _context.SaveChangesAsync(cancellationToken);

        // Publish task completed event
        await _eventBus.PublishAsync(new HumanTaskCompleted(
            task.WorkflowId,
            task.Id,
            "Returned",
            null,
            request.Comment,
            Guid.NewGuid() // CorrelationId
        ));

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> Handle(DelegateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskInstances.FindAsync(new object[] { request.TaskId }, cancellationToken);
        if (task == null) return Result<bool>.Failure("Task not found");

        if (task.AssigneeType != "User" || task.AssigneeId != request.UserId)
            return Result<bool>.Failure("Not assigned to you");

        task.AssigneeType = request.TargetAssigneeType;
        task.AssigneeId = request.TargetAssigneeId;
        task.Status = "Open"; // Re-open for new assignee

        _context.TaskActionLogs.Add(new TaskActionLog
        {
            TaskId = task.Id,
            Action = "Delegate",
            ActorId = request.UserId,
            Comment = request.Comment,
            PayloadJson = JsonSerializer.Serialize(new { to = request.TargetAssigneeId, type = request.TargetAssigneeType })
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
