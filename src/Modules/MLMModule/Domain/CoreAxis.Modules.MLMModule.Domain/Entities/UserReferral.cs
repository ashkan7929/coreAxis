using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.MLMModule.Domain.Entities;

public class UserReferral : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid? ParentUserId { get; private set; }
    public string Path { get; private set; } = string.Empty; // Materialized Path for efficient querying
    public int Level { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;
    public DateTime JoinedAt { get; private set; }
    
    // Navigation properties
    public virtual UserReferral? Parent { get; private set; }
    public virtual ICollection<UserReferral> Children { get; private set; } = new List<UserReferral>();
    public virtual ICollection<CommissionTransaction> EarnedCommissions { get; private set; } = new List<CommissionTransaction>();
    
    private UserReferral() { } // For EF Core
    
    public UserReferral(Guid userId, Guid? parentUserId)
    {
        UserId = userId;
        ParentUserId = parentUserId;
        JoinedAt = DateTime.UtcNow;
        CreatedOn = DateTime.UtcNow;
        
        // Calculate level and path based on parent
        if (parentUserId.HasValue)
        {
            // Level and Path will be set by the domain service
            Level = 1; // Will be updated by domain service
            Path = $"/{parentUserId}/"; // Will be updated by domain service
        }
        else
        {
            Level = 0;
            Path = "/";
        }
    }
    
    public void SetPath(string path, int level)
    {
        Path = path;
        Level = level;
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
    
    public List<Guid> GetUplineUserIds()
    {
        if (string.IsNullOrEmpty(Path) || Path == "/")
            return new List<Guid>();
            
        var pathParts = Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var uplineIds = new List<Guid>();
        
        foreach (var part in pathParts)
        {
            if (Guid.TryParse(part, out var uplineId))
            {
                uplineIds.Add(uplineId);
            }
        }
        
        return uplineIds;
    }
}