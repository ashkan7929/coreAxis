using CoreAxis.Modules.TaskModule.Application.DTOs;
using CoreAxis.Modules.TaskModule.Application.Queries;
using CoreAxis.Modules.TaskModule.Infrastructure.Data;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.TaskModule.Application.Handlers;

public class TaskQueryHandlers :
    IRequestHandler<GetInboxQuery, Result<List<TaskDto>>>,
    IRequestHandler<GetTaskDetailsQuery, Result<TaskDto>>,
    IRequestHandler<GetTaskHistoryQuery, Result<List<TaskActionLogDto>>>
{
    private readonly TaskDbContext _context;

    public TaskQueryHandlers(TaskDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<TaskDto>>> Handle(GetInboxQuery request, CancellationToken cancellationToken)
    {
        // Filter by Status
        var query = _context.TaskInstances.AsNoTracking();
        
        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(t => t.Status == request.Status);
        }

        // Filter by Assignee (User OR Role)
        // Logic: (AssigneeType == 'User' AND AssigneeId == UserId) OR (AssigneeType == 'Role' AND Roles.Contains(AssigneeId))
        
        var tasks = await query.ToListAsync(cancellationToken);
        
        var filteredTasks = tasks.Where(t => 
            (t.AssigneeType == "User" && t.AssigneeId == request.AssigneeId) ||
            (t.AssigneeType == "Role" && request.Roles.Contains(t.AssigneeId))
        ).ToList();

        var dtos = filteredTasks.Select(t => new TaskDto
        {
            Id = t.Id,
            WorkflowId = t.WorkflowId,
            StepKey = t.StepKey,
            Status = t.Status,
            AssigneeType = t.AssigneeType,
            AssigneeId = t.AssigneeId,
            PayloadJson = t.PayloadJson,
            AllowedActionsJson = t.AllowedActionsJson,
            CreatedAt = t.CreatedOn,
            DueAt = t.DueAt,
            CompletedAt = t.CompletedAt
        }).ToList();

        return Result<List<TaskDto>>.Success(dtos);
    }

    public async Task<Result<TaskDto>> Handle(GetTaskDetailsQuery request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskInstances.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null)
        {
            return Result<TaskDto>.Failure("Task not found");
        }

        // Check access
        bool hasAccess = (task.AssigneeType == "User" && task.AssigneeId == request.UserId) ||
                         (task.AssigneeType == "Role" && request.Roles.Contains(task.AssigneeId));

        if (!hasAccess)
        {
            // Allow read-only access if previously acted on? Or just deny?
            // For MVP, strict assignee check.
            return Result<TaskDto>.Failure("Unauthorized access to task");
        }

        return Result<TaskDto>.Success(new TaskDto
        {
            Id = task.Id,
            WorkflowId = task.WorkflowId,
            StepKey = task.StepKey,
            Status = task.Status,
            AssigneeType = task.AssigneeType,
            AssigneeId = task.AssigneeId,
            PayloadJson = task.PayloadJson,
            AllowedActionsJson = task.AllowedActionsJson,
            CreatedAt = task.CreatedOn,
            DueAt = task.DueAt,
            CompletedAt = task.CompletedAt
        });
    }

    public async Task<Result<List<TaskActionLogDto>>> Handle(GetTaskHistoryQuery request, CancellationToken cancellationToken)
    {
        var task = await _context.TaskInstances.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);

        if (task == null)
        {
            return Result<List<TaskActionLogDto>>.Failure("Task not found");
        }

        // Check access (same as details)
        bool hasAccess = (task.AssigneeType == "User" && task.AssigneeId == request.UserId) ||
                         (task.AssigneeType == "Role" && request.Roles.Contains(task.AssigneeId));

        if (!hasAccess)
        {
            return Result<List<TaskActionLogDto>>.Failure("Unauthorized access to task");
        }

        var logs = await _context.TaskActionLogs
            .Where(l => l.TaskId == request.TaskId)
            .OrderByDescending(l => l.CreatedOn)
            .Select(l => new TaskActionLogDto
            {
                Id = l.Id,
                Action = l.Action,
                ActorId = l.ActorId,
                Comment = l.Comment,
                At = l.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return Result<List<TaskActionLogDto>>.Success(logs);
    }
}
