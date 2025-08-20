namespace CoreAxis.Modules.MLMModule.Application.Contracts;

public class UserReferralInfoResponse
{
    public Guid UserId { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid? ParentUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public int DirectChildrenCount { get; set; }
    public int TotalDownlineCount { get; set; }
}