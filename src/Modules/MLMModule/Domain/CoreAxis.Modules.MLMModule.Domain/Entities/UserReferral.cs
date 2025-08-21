using CoreAxis.SharedKernel;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using System.Linq;

namespace CoreAxis.Modules.MLMModule.Domain.Entities;

public class UserReferral : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid? ParentUserId { get; private set; }
    public string Path { get; private set; } = string.Empty; // Materialized Path for efficient querying
    public string MaterializedPath { get; private set; } = string.Empty; // Alternative name for Path
    public string ReferralCode { get; private set; } = string.Empty; // Unique referral code
    public int Level { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;
    public ReferralStatus Status { get; private set; } = ReferralStatus.Active;
    public DateTime JoinedAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    
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
        Status = ReferralStatus.Active;
        ReferralCode = GenerateReferralCode();
        
        // Calculate level and path based on parent
        if (parentUserId.HasValue)
        {
            // Level and Path will be set by the domain service
            Level = 1; // Will be updated by domain service
            Path = $"/{parentUserId}/"; // Will be updated by domain service
            MaterializedPath = Path;
        }
        else
        {
            Level = 0;
            Path = "/";
            MaterializedPath = Path;
        }
    }
    
    public static UserReferral Create(Guid userId, Guid? parentUserId, string path, ReferralStatus status, DateTime joinedAt)
    {
        if (userId == parentUserId)
        {
            throw new ArgumentException("User cannot be their own parent", nameof(parentUserId));
        }
        
        var userReferral = new UserReferral
        {
            UserId = userId,
            ParentUserId = parentUserId,
            Path = path,
            MaterializedPath = path,
            Status = status,
            JoinedAt = joinedAt,
            CreatedOn = DateTime.UtcNow,
            IsActive = status == ReferralStatus.Active
        };
        
        userReferral.ReferralCode = userReferral.GenerateReferralCode();
        
        // Calculate level from path
        if (!string.IsNullOrEmpty(path) && path != "/")
        {
            var pathParts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            userReferral.Level = pathParts.Length;
        }
        else
        {
            userReferral.Level = 1;
        }
        
        return userReferral;
    }
    
    public void SetPath(string path, int level)
    {
        Path = path;
        MaterializedPath = path;
        Level = level;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    private string GenerateReferralCode()
    {
        // Generate a unique 8-character referral code
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    
    public void Activate()
    {
        IsActive = true;
        Status = ReferralStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        IsActive = false;
        Status = ReferralStatus.Inactive;
        DeactivatedAt = DateTime.UtcNow;
        LastModifiedOn = DateTime.UtcNow;
    }
    
    public int GetLevel()
    {
        if (!string.IsNullOrEmpty(Path) && Path != "/")
        {
            var pathParts = Path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return pathParts.Length;
        }
        return 1;
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