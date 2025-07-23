using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Entities;

public class RolePermission : EntityBase
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public Guid AssignedBy { get; private set; }

    // Navigation properties
    public virtual Role Role { get; private set; } = null!;
    public virtual Permission Permission { get; private set; } = null!;

    private RolePermission() { } // For EF Core

    public RolePermission(Guid roleId, Guid permissionId, Guid assignedBy)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        AssignedBy = assignedBy;
        AssignedAt = DateTime.UtcNow;
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