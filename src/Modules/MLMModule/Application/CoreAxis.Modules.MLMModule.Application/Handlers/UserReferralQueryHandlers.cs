using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Queries;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using MediatR;

namespace CoreAxis.Modules.MLMModule.Application.Handlers;

public class GetUserReferralByIdQueryHandler : IRequestHandler<GetUserReferralByIdQuery, UserReferralDto?>
{
    private readonly IUserReferralRepository _userReferralRepository;

    public GetUserReferralByIdQueryHandler(IUserReferralRepository userReferralRepository)
    {
        _userReferralRepository = userReferralRepository;
    }

    public async Task<UserReferralDto?> Handle(GetUserReferralByIdQuery request, CancellationToken cancellationToken)
    {
        var userReferral = await _userReferralRepository.GetByIdAsync(request.UserReferralId);
        if (userReferral == null || userReferral.TenantId != request.TenantId)
        {
            return null;
        }

        return new UserReferralDto
        {
            Id = userReferral.Id,
            UserId = userReferral.UserId,
            ParentUserId = userReferral.ParentUserId,
            Path = userReferral.Path,
            Level = userReferral.Level,
            IsActive = userReferral.IsActive,
            JoinedAt = userReferral.JoinedAt
        };
    }
}

public class GetUserReferralByUserIdQueryHandler : IRequestHandler<GetUserReferralByUserIdQuery, UserReferralDto?>
{
    private readonly IUserReferralRepository _userReferralRepository;

    public GetUserReferralByUserIdQueryHandler(IUserReferralRepository userReferralRepository)
    {
        _userReferralRepository = userReferralRepository;
    }

    public async Task<UserReferralDto?> Handle(GetUserReferralByUserIdQuery request, CancellationToken cancellationToken)
    {
        var userReferral = await _userReferralRepository.GetByUserIdAsync(request.UserId, request.TenantId);
        if (userReferral == null)
        {
            return null;
        }

        return new UserReferralDto
        {
            Id = userReferral.Id,
            UserId = userReferral.UserId,
            ParentUserId = userReferral.ParentUserId,
            Path = userReferral.Path,
            Level = userReferral.Level,
            IsActive = userReferral.IsActive,
            JoinedAt = userReferral.JoinedAt
        };
    }
}

public class GetUserReferralChildrenQueryHandler : IRequestHandler<GetUserReferralChildrenQuery, IEnumerable<UserReferralDto>>
{
    private readonly IUserReferralRepository _userReferralRepository;

    public GetUserReferralChildrenQueryHandler(IUserReferralRepository userReferralRepository)
    {
        _userReferralRepository = userReferralRepository;
    }

    public async Task<IEnumerable<UserReferralDto>> Handle(GetUserReferralChildrenQuery request, CancellationToken cancellationToken)
    {
        var children = await _userReferralRepository.GetChildrenAsync(request.UserId, request.TenantId, cancellationToken);
        
        return children.Select(child => new UserReferralDto
        {
            Id = child.Id,
            UserId = child.UserId,
            ParentUserId = child.ParentUserId,
            Path = child.Path,
            Level = child.Level,
            IsActive = child.IsActive,
            JoinedAt = child.JoinedAt
        });
    }
}

public class GetUserUplineQueryHandler : IRequestHandler<GetUserUplineQuery, IEnumerable<UserReferralDto>>
{
    private readonly IUserReferralRepository _userReferralRepository;

    public GetUserUplineQueryHandler(IUserReferralRepository userReferralRepository)
    {
        _userReferralRepository = userReferralRepository;
    }

    public async Task<IEnumerable<UserReferralDto>> Handle(GetUserUplineQuery request, CancellationToken cancellationToken)
    {
        var upline = await _userReferralRepository.GetUplineAsync(request.UserId, request.TenantId, request.MaxLevels ?? 10, cancellationToken);
        
        return upline.Select(user => new UserReferralDto
        {
            Id = user.Id,
            UserId = user.UserId,
            ParentUserId = user.ParentUserId,
            Path = user.Path,
            Level = user.Level,
            IsActive = user.IsActive,
            JoinedAt = user.JoinedAt
        });
    }
}

public class GetUserDownlineQueryHandler : IRequestHandler<GetUserDownlineQuery, IEnumerable<UserReferralDto>>
{
    private readonly IUserReferralRepository _userReferralRepository;

    public GetUserDownlineQueryHandler(IUserReferralRepository userReferralRepository)
    {
        _userReferralRepository = userReferralRepository;
    }

    public async Task<IEnumerable<UserReferralDto>> Handle(GetUserDownlineQuery request, CancellationToken cancellationToken)
    {
        var downline = await _userReferralRepository.GetDownlineAsync(request.UserId, request.TenantId, request.MaxLevels ?? 10, cancellationToken);
        
        return downline.Select(user => new UserReferralDto
        {
            Id = user.Id,
            UserId = user.UserId,
            ParentUserId = user.ParentUserId,
            Path = user.Path,
            Level = user.Level,
            IsActive = user.IsActive,
            JoinedAt = user.JoinedAt
        });
    }
}

public class GetMLMNetworkStatsQueryHandler : IRequestHandler<GetMLMNetworkStatsQuery, MLMNetworkStatsDto>
{
    private readonly IUserReferralRepository _userReferralRepository;
    private readonly ICommissionTransactionRepository _commissionRepository;

    public GetMLMNetworkStatsQueryHandler(
        IUserReferralRepository userReferralRepository,
        ICommissionTransactionRepository commissionRepository)
    {
        _userReferralRepository = userReferralRepository;
        _commissionRepository = commissionRepository;
    }

    public async Task<MLMNetworkStatsDto> Handle(GetMLMNetworkStatsQuery request, CancellationToken cancellationToken)
    {
        var networkSize = await _userReferralRepository.GetNetworkSizeAsync(request.UserId, request.TenantId);
        var directReferrals = await _userReferralRepository.GetChildrenAsync(request.UserId, request.TenantId, cancellationToken);
        var totalEarnings = await _commissionRepository.GetTotalEarningsAsync(request.UserId, request.TenantId);
        var pendingCommissions = await _commissionRepository.GetTotalPendingAsync(request.UserId, request.TenantId);

        return new MLMNetworkStatsDto
        {
            UserId = request.UserId,
            TotalNetworkSize = networkSize,
            DirectReferrals = directReferrals.Count(),
            TotalCommissionsEarned = totalEarnings,
            PendingCommissions = pendingCommissions,
            ActiveReferrals = directReferrals.Count(r => r.IsActive),
            MaxDepth = 0 // TODO: Calculate actual max depth
        };
    }
}

public class GetNetworkTreeQueryHandler : IRequestHandler<GetNetworkTreeQuery, NetworkTreeNodeDto?>
{
    private readonly IUserReferralRepository _userReferralRepository;

    public GetNetworkTreeQueryHandler(IUserReferralRepository userReferralRepository)
    {
        _userReferralRepository = userReferralRepository;
    }

    public async Task<NetworkTreeNodeDto?> Handle(GetNetworkTreeQuery request, CancellationToken cancellationToken)
    {
        var rootUser = await _userReferralRepository.GetByUserIdAsync(request.UserId, request.TenantId);
        if (rootUser == null)
        {
            return null;
        }

        return await BuildNetworkTree(rootUser, request.MaxDepth, request.ActiveOnly, request.TenantId, 0);
    }

    private async Task<NetworkTreeNodeDto> BuildNetworkTree(
        Domain.Entities.UserReferral user, 
        int maxDepth, 
        bool activeOnly, 
        Guid tenantId, 
        int currentDepth)
    {
        var node = new NetworkTreeNodeDto
        {
            UserId = user.UserId,
            Level = user.Level,
            IsActive = user.IsActive,
            JoinedAt = user.JoinedAt,
            Children = new List<NetworkTreeNodeDto>()
        };

        if (currentDepth < maxDepth)
        {
            var children = await _userReferralRepository.GetChildrenAsync(user.UserId, tenantId);
            foreach (var child in children)
            {
                var childNode = await BuildNetworkTree(child, maxDepth, activeOnly, tenantId, currentDepth + 1);
                node.Children.Add(childNode);
            }
        }

        return node;
    }
}