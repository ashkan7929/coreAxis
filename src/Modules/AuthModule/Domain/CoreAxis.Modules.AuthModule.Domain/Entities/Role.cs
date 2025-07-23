using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Entities;

public class Role : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; } = false;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private Role() { } // For EF Core

    public Role(string name, Guid tenantId, string? description = null, bool isSystemRole = false)
    {
        Name = name;
        Description = description;
        TenantId = tenantId;
        IsSystemRole = isSystemRole;
    }

    public void UpdateDetails(string name, string? description = null)
    {
        Name = name;
        Description = description;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (IsSystemRole)
            throw new InvalidOperationException("Cannot deactivate system roles");
            
        IsActive = false;
        LastModifiedOn = DateTime.UtcNow;
    }
}