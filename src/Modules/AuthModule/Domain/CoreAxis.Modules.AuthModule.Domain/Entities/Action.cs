using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Entities;

public class Action : EntityBase
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; } = 0;

    // Navigation properties
    public virtual ICollection<Permission> Permissions { get; private set; } = new List<Permission>();

    private Action() { } // For EF Core

    public Action(string code, string name, string? description = null, int sortOrder = 0)
    {
        Code = code;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
    }

    public void UpdateDetails(string name, string? description = null, int sortOrder = 0)
    {
        Name = name;
        Description = description;
        SortOrder = sortOrder;
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