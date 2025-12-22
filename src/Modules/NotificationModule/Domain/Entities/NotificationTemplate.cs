using CoreAxis.Modules.NotificationModule.Domain.Enums;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.NotificationModule.Domain.Entities;

public class NotificationTemplate : EntityBase
{
    public string Key { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string? Description { get; set; }
}
