using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.AuthModule.Domain.Entities;

public class Page : EntityBase
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Path { get; private set; }
    public string ModuleName { get; private set; } = string.Empty;
    public int SortOrder { get; private set; } = 0;

    // Navigation properties
    public virtual ICollection<Permission> Permissions { get; private set; } = new List<Permission>();

    private Page() { } // For EF Core

    public Page(string code, string name, string moduleName, string? description = null, string? path = null, int sortOrder = 0)
    {
        Code = code;
        Name = name;
        Description = description;
        Path = path;
        ModuleName = moduleName;
        SortOrder = sortOrder;
    }

    public void UpdateDetails(string name, string? description = null, string? path = null, int sortOrder = 0)
    {
        Name = name;
        Description = description;
        Path = path;
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