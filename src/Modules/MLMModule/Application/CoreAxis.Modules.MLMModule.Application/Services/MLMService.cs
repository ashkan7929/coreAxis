using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.SharedKernel.Domain;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.MLMModule.Application.Services;

public class MLMService : IMLMService
{
    private readonly IUserReferralRepository _userReferralRepository;
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly ICommissionTransactionRepository _commissionRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<MLMService> _logger;

    public MLMService(
        IUserReferralRepository userReferralRepository,
        ICommissionRuleSetRepository ruleSetRepository,
        ICommissionTransactionRepository commissionRepository,
        IDomainEventDispatcher eventDispatcher,
        ILogger<MLMService> logger)
    {
        _userReferralRepository = userReferralRepository;
        _ruleSetRepository = ruleSetRepository;
        _commissionRepository = commissionRepository;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<IEnumerable<CommissionTransactionDto>> ProcessCommissionAsync(
        Guid sourcePaymentId,
        Guid productId,
        decimal amount,
        Guid buyerUserId,
        Guid tenantId)
    {
        _logger.LogInformation("Processing commission for payment {PaymentId}, product {ProductId}, amount {Amount}", 
            sourcePaymentId, productId, amount);

        // Get commission rule set for the product
        var ruleSet = await _ruleSetRepository.GetByProductIdAsync(productId, tenantId);
        if (ruleSet == null)
        {
            // Fall back to default rule set
            ruleSet = await _ruleSetRepository.GetDefaultAsync(tenantId);
        }

        if (ruleSet == null || !ruleSet.IsActive)
        {
            _logger.LogWarning("No active commission rule set found for product {ProductId} or tenant {TenantId}", 
                productId, tenantId);
            return Enumerable.Empty<CommissionTransactionDto>();
        }

        // Get upline chain for the buyer
        var uplineChain = await GetUplineChainAsync(buyerUserId, tenantId, ruleSet.MaxLevels);
        var uplineList = uplineChain.ToList();

        if (!uplineList.Any())
        {
            _logger.LogInformation("No upline found for user {UserId}", buyerUserId);
            return Enumerable.Empty<CommissionTransactionDto>();
        }

        var commissions = new List<CommissionTransaction>();
        var activeLevels = ruleSet.CommissionLevels?.Where(l => l.IsActive).OrderBy(l => l.Level).ToList() ?? new List<CommissionLevel>();

        // Process commissions for each level
        for (int i = 0; i < Math.Min(uplineList.Count, activeLevels.Count); i++)
        {
            var uplineUser = uplineList[i];
            var level = activeLevels[i];

            // Check if upline user is active
            if (!uplineUser.IsActive)
            {
                _logger.LogDebug("Skipping inactive upline user {UserId} at level {Level}", 
                    uplineUser.UserId, level.Level);
                continue;
            }

            // Calculate commission amount
            var commissionAmount = amount * (level.Percentage / 100);

            // Apply maximum commission limit if set
            if (level.MaxAmount.HasValue && commissionAmount > level.MaxAmount.Value)
            {
                commissionAmount = level.MaxAmount.Value;
            }

            // Apply minimum commission limit if set
            if (level.MinAmount.HasValue && commissionAmount < level.MinAmount.Value)
            {
                commissionAmount = level.MinAmount.Value;
            }

            var commission = new CommissionTransaction(
                uplineUser.UserId,
                sourcePaymentId,
                ruleSet.Id,
                level.Level,
                commissionAmount,
                amount,
                level.Percentage,
                tenantId,
                productId);

            commissions.Add(commission);
            await _commissionRepository.AddAsync(commission);

            _logger.LogDebug("Created commission for user {UserId} at level {Level}: {Amount}", 
                uplineUser.UserId, level.Level, commissionAmount);
        }

        // Dispatch domain events
        foreach (var commission in commissions)
        {
            await _eventDispatcher.DispatchAsync(commission.DomainEvents);
        }

        return commissions.Select(MapToDto);
    }

    public async Task<IEnumerable<CommissionCalculationDto>> CalculatePotentialCommissionsAsync(
        Guid productId,
        decimal amount,
        Guid buyerUserId,
        Guid tenantId)
    {
        // Get commission rule set for the product
        var ruleSet = await _ruleSetRepository.GetByProductIdAsync(productId, tenantId);
        if (ruleSet == null)
        {
            ruleSet = await _ruleSetRepository.GetDefaultAsync(tenantId);
        }

        if (ruleSet == null || !ruleSet.IsActive)
        {
            return Enumerable.Empty<CommissionCalculationDto>();
        }

        // Get upline chain
        var uplineChain = await GetUplineChainAsync(buyerUserId, tenantId, ruleSet.MaxLevels);
        var uplineList = uplineChain.ToList();

        var calculations = new List<CommissionCalculationDto>();
        var activeLevels = ruleSet.CommissionLevels?.Where(l => l.IsActive).OrderBy(l => l.Level).ToList() ?? new List<CommissionLevel>();

        for (int i = 0; i < Math.Min(uplineList.Count, activeLevels.Count); i++)
        {
            var uplineUser = uplineList[i];
            var level = activeLevels[i];

            if (!uplineUser.IsActive) continue;

            var commissionAmount = amount * (level.Percentage / 100);

            if (level.MaxAmount.HasValue && commissionAmount > level.MaxAmount.Value)
            {
                commissionAmount = level.MaxAmount.Value;
            }

            calculations.Add(new CommissionCalculationDto
            {
                UserId = uplineUser.UserId,
                Level = level.Level,
                Amount = commissionAmount,
                Percentage = level.Percentage,
                SourceAmount = amount,
                UserName = string.Empty, // UserName not available in UserReferral entity
                UserEmail = string.Empty // UserEmail not available in UserReferral entity
            });
        }

        return calculations;
    }

    public async Task<NetworkTreeNodeDto> BuildNetworkTreeAsync(
        Guid userId,
        Guid tenantId,
        int maxDepth = 10)
    {
        var userReferral = await _userReferralRepository.GetByUserIdAsync(userId, tenantId);
        if (userReferral == null)
        {
            throw new InvalidOperationException($"User referral not found for user {userId}");
        }

        return await BuildNetworkTreeRecursiveAsync(userReferral, tenantId, 0, maxDepth);
    }

    public async Task<MLMNetworkStatsDto> GetNetworkStatsAsync(Guid userId, Guid tenantId)
    {
        var userReferral = await _userReferralRepository.GetByUserIdAsync(userId, tenantId);
        if (userReferral == null)
        {
            return new MLMNetworkStatsDto
            {
                UserId = userId,
                DirectReferrals = 0,
                TotalNetworkSize = 0,
                ActiveReferrals = 0,
                TotalCommissionsEarned = 0,
                PendingCommissions = 0,
                MaxDepth = 0
            };
        }

        var directChildren = await _userReferralRepository.GetChildrenAsync(userId, tenantId);
        var directChildrenList = directChildren.ToList();
        
        var totalNetworkSize = await _userReferralRepository.GetNetworkSizeAsync(userId, tenantId);
        var activeReferrals = directChildrenList.Count(c => c.IsActive);
        
        var totalCommissions = await _commissionRepository.GetTotalEarningsAsync(userId, tenantId);
        var pendingCommissions = await _commissionRepository.GetTotalPendingAsync(userId, tenantId);
        
        var networkDepth = await CalculateNetworkDepthAsync(userId, tenantId);

        return new MLMNetworkStatsDto
        {
            UserId = userId,
            DirectReferrals = directChildrenList.Count,
            TotalNetworkSize = totalNetworkSize,
            ActiveReferrals = activeReferrals,
            TotalCommissionsEarned = totalCommissions,
            PendingCommissions = pendingCommissions,
            MaxDepth = networkDepth
        };
    }

    public async Task<bool> ValidateReferralRelationshipAsync(
        Guid parentUserId,
        Guid childUserId,
        Guid tenantId)
    {
        // Cannot refer yourself
        if (parentUserId == childUserId)
        {
            return false;
        }

        // Check if child already has a parent
        var existingChild = await _userReferralRepository.GetByUserIdAsync(childUserId, tenantId);
        if (existingChild != null)
        {
            return false;
        }

        // Check if parent exists and is active
        var parent = await _userReferralRepository.GetByUserIdAsync(parentUserId, tenantId);
        if (parent == null || !parent.IsActive)
        {
            return false;
        }

        // Check for circular reference (child cannot be in parent's upline)
        var parentUpline = await GetUplineChainAsync(parentUserId, tenantId);
        if (parentUpline.Any(u => u.UserId == childUserId))
        {
            return false;
        }

        return true;
    }

    public async Task<IEnumerable<UserReferralDto>> GetUplineChainAsync(
        Guid userId,
        Guid tenantId,
        int maxLevels = 10)
    {
        var uplineChain = new List<UserReferralDto>();
        var currentUserId = userId;
        var level = 0;

        while (level < maxLevels)
        {
            var userReferral = await _userReferralRepository.GetByUserIdAsync(currentUserId, tenantId);
            if (userReferral?.ParentUserId == null)
            {
                break;
            }

            var parentReferral = await _userReferralRepository.GetByUserIdAsync(userReferral.ParentUserId.Value, tenantId);
            if (parentReferral == null)
            {
                break;
            }

            uplineChain.Add(MapToDto(parentReferral));
            currentUserId = parentReferral.UserId;
            level++;
        }

        return uplineChain;
    }

    public async Task<int> ProcessExpiredCommissionsAsync(Guid tenantId, DateTime expirationDate)
    {
        var pendingCommissions = await _commissionRepository.GetByStatusAsync(
            CommissionStatus.Pending, tenantId);

        var expiredCommissions = pendingCommissions
            .Where(c => c.CreatedOn <= expirationDate)
            .ToList();

        foreach (var commission in expiredCommissions)
        {
            commission.MarkAsExpired();
            await _commissionRepository.UpdateAsync(commission);
            await _eventDispatcher.DispatchAsync(commission.DomainEvents);
        }

        _logger.LogInformation("Processed {Count} expired commissions for tenant {TenantId}", 
            expiredCommissions.Count, tenantId);

        return expiredCommissions.Count;
    }

    private async Task<NetworkTreeNodeDto> BuildNetworkTreeRecursiveAsync(
        UserReferral userReferral,
        Guid tenantId,
        int currentDepth,
        int maxDepth)
    {
        var node = new NetworkTreeNodeDto
        {
            UserId = userReferral.UserId,
            Level = currentDepth,
            IsActive = userReferral.IsActive,
            JoinedAt = userReferral.CreatedOn,
            Children = new List<NetworkTreeNodeDto>()
        };

        if (currentDepth < maxDepth)
        {
            var children = await _userReferralRepository.GetChildrenAsync(userReferral.UserId, tenantId);
            foreach (var child in children)
            {
                var childNode = await BuildNetworkTreeRecursiveAsync(child, tenantId, currentDepth + 1, maxDepth);
                node.Children.Add(childNode);
            }
        }

        return node;
    }

    private async Task<int> CalculateNetworkDepthAsync(Guid userId, Guid tenantId)
    {
        return await CalculateDepthRecursiveAsync(userId, tenantId, 0);
    }

    private async Task<int> CalculateDepthRecursiveAsync(Guid userId, Guid tenantId, int currentDepth)
    {
        var maxDepth = currentDepth;
        
        var children = await _userReferralRepository.GetChildrenAsync(userId, tenantId);
        foreach (var child in children)
        {
            var childDepth = await CalculateDepthRecursiveAsync(child.UserId, tenantId, currentDepth + 1);
            maxDepth = Math.Max(maxDepth, childDepth);
        }
        
        return maxDepth;
    }

    private async Task<decimal> GetUserTotalSalesAsync(Guid userId, Guid tenantId)
    {
        // This would typically integrate with a sales/order system
        // For now, we'll return the total commission earnings as a proxy
        return await _commissionRepository.GetTotalEarningsAsync(userId, tenantId);
    }

    private static CommissionTransactionDto MapToDto(CommissionTransaction commission)
    {
        return new CommissionTransactionDto
        {
            Id = commission.Id,
            UserId = commission.UserId,
            SourcePaymentId = commission.SourcePaymentId,
            ProductId = commission.ProductId,
            CommissionRuleSetId = commission.CommissionRuleSetId,
            Level = commission.Level,
            Amount = commission.Amount,
            SourceAmount = commission.SourceAmount,
            Percentage = commission.Percentage,
            Status = commission.Status,
            IsSettled = commission.IsSettled,
            WalletTransactionId = commission.WalletTransactionId,
            Notes = commission.Notes,
            CreatedOn = commission.CreatedOn,
            ApprovedAt = commission.ApprovedAt,
            PaidAt = commission.PaidAt,
            RejectedAt = commission.RejectedAt,
            RejectionReason = commission.RejectionReason
        };
    }

    private static UserReferralDto MapToDto(UserReferral userReferral)
    {
        return new UserReferralDto
        {
            Id = userReferral.Id,
            UserId = userReferral.UserId,
            ParentUserId = userReferral.ParentUserId,
            Path = userReferral.Path,
            Level = userReferral.Level,
            IsActive = userReferral.IsActive,
            JoinedAt = userReferral.JoinedAt,
            CreatedOn = userReferral.CreatedOn
        };
    }
}