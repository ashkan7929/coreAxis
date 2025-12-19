using CoreAxis.Modules.TaskModule.Application.DTOs;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.TaskModule.Application.Queries;

public record GetInboxQuery(string AssigneeId, List<string> Roles, string? Status = "Open") : IRequest<Result<List<TaskDto>>>;

public record GetTaskDetailsQuery(Guid TaskId, string UserId, List<string> Roles) : IRequest<Result<TaskDto>>;

public record GetTaskHistoryQuery(Guid TaskId, string UserId, List<string> Roles) : IRequest<Result<List<TaskActionLogDto>>>;
