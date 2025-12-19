using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.TaskModule.Domain.Entities;

public class TaskActionLog : EntityBase
{
    public Guid TaskId { get; set; }
    public string Action { get; set; } = null!;
    public string ActorId { get; set; } = null!;
    public string? Comment { get; set; }
    public string? PayloadJson { get; set; }
    
    public TaskInstance Task { get; set; } = null!;
}
