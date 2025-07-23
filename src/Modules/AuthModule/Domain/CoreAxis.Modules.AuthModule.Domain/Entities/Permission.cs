using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Entities;

public class Permission : EntityBase
{
    public Guid PageId { get; private set; }
    public Guid ActionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // Navigation properties
    public virtual Page Page { get; private set; } = null!;
    public virtual Action Action { get; private set; } = null!;
    public virtual ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
    public virtual ICollection<UserPermission> UserPermissions { get; private set; } = new List<UserPermission>();

    private Permission() { } // For EF Core

    public Permission(Guid pageId, Guid actionId, string? description = null)
    {
        PageId = pageId;
        ActionId = actionId;
        Description = description;
        // Name will be set based on Page.Code + Action.Code in the application layer
        Name = string.Empty;
    }

    public void SetName(string name)
    {
        Name = name;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void UpdateDescription(string newDescription)
    {
        Description = newDescription;
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

    public string GetPermissionCode()
    {
        return $"{Page?.Code}:{Action?.Code}";
    }
}