using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.TaskModule.Domain.Entities;

public class TaskComment : EntityBase
{
    public Guid TaskId { get; set; }
    public string AuthorId { get; set; } = null!;
    public string Text { get; set; } = null!;
    
    public TaskInstance Task { get; set; } = null!;
}
