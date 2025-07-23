using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Entities;

public class UserPermission : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public Guid AssignedBy { get; private set; }
    public bool IsGranted { get; private set; } = true; // true = grant, false = deny

    // Navigation properties
    public virtual User User { get; private set; } = null!;
    public virtual Permission Permission { get; private set; } = null!;

    private UserPermission() { } // For EF Core

    public UserPermission(Guid userId, Guid permissionId, Guid assignedBy, bool isGranted = true)
    {
        UserId = userId;
        PermissionId = permissionId;
        AssignedBy = assignedBy;
        IsGranted = isGranted;
        AssignedAt = DateTime.UtcNow;
    }

    public void Grant()
    {
        IsGranted = true;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void Deny()
    {
        IsGranted = false;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOn = DateTime.UtcNow;
    }
}