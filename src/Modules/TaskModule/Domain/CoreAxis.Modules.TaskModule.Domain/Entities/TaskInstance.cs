using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.TaskModule.Domain.Entities;

public class TaskInstance : EntityBase
{
    public Guid WorkflowId { get; set; }
    public string StepKey { get; set; } = null!;
    public string Status { get; set; } = "Open"; // Open, Assigned, Completed, Cancelled
    public string AssigneeType { get; set; } = null!; // User, Role
    public string AssigneeId { get; set; } = null!;
    public string? PayloadJson { get; set; }
    public string? AllowedActionsJson { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public ICollection<TaskActionLog> ActionLogs { get; set; } = new List<TaskActionLog>();
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
}
