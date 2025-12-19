namespace CoreAxis.Modules.TaskModule.Application.DTOs;

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string StepKey { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string AssigneeType { get; set; } = null!;
    public string AssigneeId { get; set; } = null!;
    public string? PayloadJson { get; set; }
    public string? AllowedActionsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class TaskActionLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = null!;
    public string ActorId { get; set; } = null!;
    public string? Comment { get; set; }
    public DateTime At { get; set; }
}

public class TaskActionRequest
{
    public string? Comment { get; set; }
    public Dictionary<string, object>? Payload { get; set; }
}

public class DelegateTaskRequest
{
    public string AssigneeType { get; set; } = null!; // User, Role
    public string AssigneeId { get; set; } = null!;
    public string? Comment { get; set; }
}
