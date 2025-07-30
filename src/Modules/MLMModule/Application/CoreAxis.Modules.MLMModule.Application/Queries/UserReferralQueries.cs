using CoreAxis.Modules.MLMModule.Application.DTOs;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Queries;

public class GetUserReferralByIdQuery : IRequest<UserReferralDto?>
{
    public Guid UserReferralId { get; set; }
}

public class GetUserReferralByUserIdQuery : IRequest<UserReferralDto?>
{
    public Guid UserId { get; set; }
}

public class GetUserReferralChildrenQuery : IRequest<IEnumerable<UserReferralDto>>
{
    public Guid UserId { get; set; }
    public int? MaxLevel { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public class GetUserUplineQuery : IRequest<IEnumerable<UserReferralDto>>
{
    public Guid UserId { get; set; }
    public int? MaxLevels { get; set; }
}

public class GetUserDownlineQuery : IRequest<IEnumerable<UserReferralDto>>
{
    public Guid UserId { get; set; }
    public int? MaxLevels { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public class GetMLMNetworkStatsQuery : IRequest<MLMNetworkStatsDto>
{
    public Guid UserId { get; set; }
}

public class GetNetworkTreeQuery : IRequest<NetworkTreeNodeDto?>
{
    public Guid UserId { get; set; }
    public int MaxDepth { get; set; } = 5;
    public bool ActiveOnly { get; set; } = true;
}