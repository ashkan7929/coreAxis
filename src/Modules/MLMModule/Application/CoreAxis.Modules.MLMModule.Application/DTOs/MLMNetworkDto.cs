namespace CoreAxis.Modules.MLMModule.Application.DTOs;

public class UserReferralDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentUserId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string MaterializedPath { get; set; } = string.Empty;
    public string ReferralCode { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class CreateUserReferralDto
{
    public Guid UserId { get; set; }
    public Guid? ParentUserId { get; set; }
}

public class UpdateUserReferralDto
{
    public Guid? ParentUserId { get; set; }
}

public class NetworkTreeDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Level { get; set; }
    public DateTime JoinedDate { get; set; }
    public List<NetworkTreeDto> Children { get; set; } = new();
    public int TotalDownlineCount { get; set; }
    public decimal TotalCommissions { get; set; }
}

public class MLMNetworkStatsDto
{
    public Guid UserId { get; set; }
    public int DirectReferrals { get; set; }
    public int TotalNetworkSize { get; set; }
    public int ActiveReferrals { get; set; }
    public int MaxDepth { get; set; }
    public decimal TotalCommissionsEarned { get; set; }
    public decimal PendingCommissions { get; set; }
}

public class NetworkTreeNodeDto
{
    public Guid UserId { get; set; }
    public Guid? ParentUserId { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public List<NetworkTreeNodeDto> Children { get; set; } = new();
    public decimal TotalCommissions { get; set; }
    public int DirectReferrals { get; set; }
}