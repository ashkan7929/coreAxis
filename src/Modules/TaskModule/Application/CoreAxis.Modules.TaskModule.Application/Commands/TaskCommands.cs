using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.TaskModule.Application.Commands;

public record ClaimTaskCommand(Guid TaskId, string UserId) : IRequest<Result<bool>>;

public record ApproveTaskCommand(Guid TaskId, string UserId, string? Comment, Dictionary<string, object>? Payload) : IRequest<Result<bool>>;

public record RejectTaskCommand(Guid TaskId, string UserId, string? Comment) : IRequest<Result<bool>>;

public record ReturnTaskCommand(Guid TaskId, string UserId, string? Comment) : IRequest<Result<bool>>;

public record DelegateTaskCommand(Guid TaskId, string UserId, string TargetAssigneeType, string TargetAssigneeId, string? Comment) : IRequest<Result<bool>>;
