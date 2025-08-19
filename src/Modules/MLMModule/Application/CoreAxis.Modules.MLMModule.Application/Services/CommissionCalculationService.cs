using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Enums;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;

namespace CoreAxis.Modules.MLMModule.Application.Services;

public class CommissionCalculationService : ICommissionCalculationService
{
    private readonly ICommissionRuleSetRepository _ruleSetRepository;
    private readonly IUserReferralRepository _userReferralRepository;
    private readonly ICommissionTransactionRepository _commissionRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<CommissionCalculationService> _logger;

    public CommissionCalculationService(
        ICommissionRuleSetRepository ruleSetRepository,
        IUserReferralRepository userReferralRepository,
        ICommissionTransactionRepository commissionRepository,
        IDomainEventDispatcher eventDispatcher,
        ILogger<CommissionCalculationService> logger)
    {
        _ruleSetRepository = ruleSetRepository;
        _userReferralRepository = userReferralRepository;
        _commissionRepository = commissionRepository;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<Result<List<CommissionTransactionDto>>> ProcessPaymentConfirmedAsync(
        Guid sourcePaymentId,
        Guid productId,
        decimal amount,
        Guid buyerUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing PaymentConfirmed for payment {PaymentId}, product {ProductId}, amount {Amount}, buyer {BuyerId}",
                sourcePaymentId, productId, amount, buyerUserId);

            // Check for idempotency - prevent duplicate processing
            var existingCommissions = await _commissionRepository.GetBySourcePaymentIdAsync(sourcePaymentId);
            if (existingCommissions.Any())
            {
                _logger.LogWarning("Commission already processed for payment {PaymentId}", sourcePaymentId);
                return Result<List<CommissionTransactionDto>>.Success(
                    existingCommissions.Select(MapToDto).ToList());
            }

            // Validate commission eligibility
            var eligibilityResult = await ValidateCommissionEligibilityAsync(productId, amount, buyerUserId, cancellationToken);
            if (!eligibilityResult.IsSuccess || !eligibilityResult.Value)
            {
                _logger.LogInformation("Payment {PaymentId} is not eligible for commission: {Reason}",
                sourcePaymentId, eligibilityResult.Errors.FirstOrDefault() ?? "Not eligible");
                return Result<List<CommissionTransactionDto>>.Success(new List<CommissionTransactionDto>());
            }

            // Get applicable commission rule set
            var ruleSet = await GetApplicableRuleSetAsync(productId);
            if (ruleSet == null)
            {
                _logger.LogWarning("No applicable commission rule set found for product {ProductId}", productId);
                return Result<List<CommissionTransactionDto>>.Success(new List<CommissionTransactionDto>());
            }

            // Get buyer's upline chain using materialized path
            var uplineChain = await GetUplineChainAsync(buyerUserId, ruleSet.MaxLevels);
            if (!uplineChain.Any())
            {
                _logger.LogInformation("No upline found for buyer {BuyerId}", buyerUserId);
                return Result<List<CommissionTransactionDto>>.Success(new List<CommissionTransactionDto>());
            }

            // Calculate and create commission transactions
            var commissions = await CreateCommissionTransactionsAsync(
                ruleSet, uplineChain, sourcePaymentId, productId, amount, correlationId);

            // Save all commission transactions
            foreach (var commission in commissions)
            {
                await _commissionRepository.AddAsync(commission);
            }

            // Dispatch domain events for each commission
            foreach (var commission in commissions)
            {
                await _eventDispatcher.DispatchAsync(commission.DomainEvents);
            }

            _logger.LogInformation("Successfully processed {Count} commission transactions for payment {PaymentId}",
                commissions.Count, sourcePaymentId);

            return Result<List<CommissionTransactionDto>>.Success(
                commissions.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentConfirmed for payment {PaymentId}", sourcePaymentId);
            return Result<List<CommissionTransactionDto>>.Failure($"Failed to process commission: {ex.Message}");
        }
    }

    public async Task<Result<List<CommissionCalculationDto>>> CalculatePotentialCommissionsAsync(
        Guid productId,
        decimal amount,
        Guid buyerUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get applicable commission rule set
            var ruleSet = await GetApplicableRuleSetAsync(productId);
            if (ruleSet == null)
            {
                return Result<List<CommissionCalculationDto>>.Success(new List<CommissionCalculationDto>());
            }

            // Get buyer's upline chain
            var uplineChain = await GetUplineChainAsync(buyerUserId, ruleSet.MaxLevels);
            if (!uplineChain.Any())
            {
                return Result<List<CommissionCalculationDto>>.Success(new List<CommissionCalculationDto>());
            }

            // Calculate potential commissions
            var calculations = CalculateCommissions(ruleSet, uplineChain, amount);

            return Result<List<CommissionCalculationDto>>.Success(calculations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating potential commissions for product {ProductId}", productId);
            return Result<List<CommissionCalculationDto>>.Failure($"Failed to calculate commissions: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ValidateCommissionEligibilityAsync(
        Guid productId,
        decimal amount,
        Guid buyerUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if buyer exists in referral system
            var buyerReferral = await _userReferralRepository.GetByUserIdAsync(buyerUserId);
            if (buyerReferral == null)
            {
                return Result<bool>.Failure("Buyer is not part of the referral system");
            }

            // Check if buyer is active
            if (!buyerReferral.IsActive)
            {
                return Result<bool>.Failure("Buyer account is not active");
            }

            // Get applicable rule set
            var ruleSet = await GetApplicableRuleSetAsync(productId);
            if (ruleSet == null)
            {
                return Result<bool>.Failure("No applicable commission rule set found");
            }

            // Check minimum purchase amount
            if (ruleSet.MinimumPurchaseAmount > 0 && amount < ruleSet.MinimumPurchaseAmount)
            {
                return Result<bool>.Failure($"Purchase amount {amount} is below minimum required {ruleSet.MinimumPurchaseAmount}");
            }

            // Check if rule set is active
            if (!ruleSet.IsActive)
            {
                return Result<bool>.Failure("Commission rule set is not active");
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating commission eligibility for product {ProductId}", productId);
            return Result<bool>.Failure($"Failed to validate eligibility: {ex.Message}");
        }
    }

    private async Task<CommissionRuleSet?> GetApplicableRuleSetAsync(Guid productId)
    {
        // First try to get product-specific rule set
        var ruleSet = await _ruleSetRepository.GetByProductIdAsync(productId);
        if (ruleSet != null && ruleSet.IsActive)
        {
            return ruleSet;
        }

        // Fall back to default rule set
        ruleSet = await _ruleSetRepository.GetDefaultAsync();
        if (ruleSet != null && ruleSet.IsActive)
        {
            return ruleSet;
        }

        return null;
    }

    private async Task<List<UserReferral>> GetUplineChainAsync(Guid userId, int maxLevels)
    {
        var uplineChain = new List<UserReferral>();
        var currentUser = await _userReferralRepository.GetByUserIdAsync(userId);
        
        if (currentUser?.ParentUserId == null)
        {
            return uplineChain;
        }

        // Use materialized path to get upline efficiently
        var uplineUsers = await _userReferralRepository.GetUplineAsync(userId, maxLevels);
        
        // Filter active users and sort by level (closest parent first)
        uplineChain = uplineUsers
            .Where(u => u.IsActive)
            .OrderBy(u => u.Level)
            .Take(maxLevels)
            .ToList();

        return uplineChain;
    }

    private async Task<List<CommissionTransaction>> CreateCommissionTransactionsAsync(
        CommissionRuleSet ruleSet,
        List<UserReferral> uplineChain,
        Guid sourcePaymentId,
        Guid productId,
        decimal amount,
        string correlationId)
    {
        var commissions = new List<CommissionTransaction>();
        
        // Get active commission levels from the latest version
        var latestVersion = ruleSet.Versions
            .Where(v => v.IsActive)
            .OrderByDescending(v => v.Version)
            .FirstOrDefault();

        if (latestVersion?.SchemaJson == null)
        {
            _logger.LogWarning("No active version found for rule set {RuleSetId}", ruleSet.Id);
            return commissions;
        }

        // Parse commission levels from schema JSON
        var commissionLevels = ParseCommissionLevelsFromSchema(latestVersion.SchemaJson);
        if (!commissionLevels.Any())
        {
            _logger.LogWarning("No commission levels found in rule set {RuleSetId} version {Version}", 
                ruleSet.Id, latestVersion.Version);
            return commissions;
        }

        // Create commission for each eligible upline user
        for (int i = 0; i < Math.Min(uplineChain.Count, commissionLevels.Count); i++)
        {
            var uplineUser = uplineChain[i];
            var levelConfig = commissionLevels[i];
            
            // Skip if upline user is not active
            if (!uplineUser.IsActive)
            {
                _logger.LogDebug("Skipping inactive upline user {UserId} at level {Level}", 
                    uplineUser.UserId, levelConfig.Level);
                continue;
            }

            // Check if upline is required to be active (business rule)
            if (ruleSet.RequireActiveUpline && !await IsUplineActiveAsync(uplineUser.UserId))
            {
                _logger.LogDebug("Skipping inactive upline user {UserId} (require active upline enabled)", 
                    uplineUser.UserId);
                continue;
            }

            // Calculate commission amount
            var commissionAmount = CalculateCommissionAmount(amount, levelConfig);
            if (commissionAmount <= 0)
            {
                continue;
            }

            // Create commission transaction
            var commission = new CommissionTransaction(
                uplineUser.UserId,
                sourcePaymentId,
                ruleSet.Id,
                ruleSet.Code,
                latestVersion.Version,
                levelConfig.Level,
                commissionAmount,
                amount,
                levelConfig.Percentage,
                correlationId,
                productId);

            commissions.Add(commission);

            _logger.LogDebug("Created commission for user {UserId} at level {Level}: {Amount}", 
                uplineUser.UserId, levelConfig.Level, commissionAmount);
        }

        return commissions;
    }

    private List<CommissionLevelConfig> ParseCommissionLevelsFromSchema(string schemaJson)
    {
        try
        {
            var schema = JsonSerializer.Deserialize<CommissionRuleSchema>(schemaJson);
            return schema?.Rules?.Where(r => r.Level > 0 && r.Percentage > 0)
                .OrderBy(r => r.Level)
                .Select(r => new CommissionLevelConfig
                {
                    Level = r.Level,
                    Percentage = r.Percentage,
                    MaxAmount = r.MaxAmount,
                    MinAmount = r.MinAmount
                })
                .ToList() ?? new List<CommissionLevelConfig>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing commission levels from schema: {Schema}", schemaJson);
            return new List<CommissionLevelConfig>();
        }
    }

    private decimal CalculateCommissionAmount(decimal sourceAmount, CommissionLevelConfig levelConfig)
    {
        var commissionAmount = sourceAmount * (levelConfig.Percentage / 100);

        // Apply maximum limit if set
        if (levelConfig.MaxAmount.HasValue && commissionAmount > levelConfig.MaxAmount.Value)
        {
            commissionAmount = levelConfig.MaxAmount.Value;
        }

        // Apply minimum limit if set
        if (levelConfig.MinAmount.HasValue && commissionAmount < levelConfig.MinAmount.Value)
        {
            commissionAmount = levelConfig.MinAmount.Value;
        }

        return Math.Round(commissionAmount, 6); // Round to 6 decimal places for precision
    }

    private List<CommissionCalculationDto> CalculateCommissions(
        CommissionRuleSet ruleSet,
        List<UserReferral> uplineChain,
        decimal amount)
    {
        var calculations = new List<CommissionCalculationDto>();
        
        // Get active commission levels from the latest version
        var latestVersion = ruleSet.Versions
            .Where(v => v.IsActive)
            .OrderByDescending(v => v.Version)
            .FirstOrDefault();

        if (latestVersion?.SchemaJson == null)
        {
            return calculations;
        }

        var commissionLevels = ParseCommissionLevelsFromSchema(latestVersion.SchemaJson);
        
        for (int i = 0; i < Math.Min(uplineChain.Count, commissionLevels.Count); i++)
        {
            var uplineUser = uplineChain[i];
            var levelConfig = commissionLevels[i];
            
            if (!uplineUser.IsActive) continue;

            var commissionAmount = CalculateCommissionAmount(amount, levelConfig);
            if (commissionAmount <= 0) continue;

            calculations.Add(new CommissionCalculationDto
            {
                UserId = uplineUser.UserId,
                Level = levelConfig.Level,
                Amount = commissionAmount,
                Percentage = levelConfig.Percentage,
                SourceAmount = amount,
                UserName = string.Empty, // Would need user service integration
                UserEmail = string.Empty // Would need user service integration
            });
        }

        return calculations;
    }

    private async Task<bool> IsUplineActiveAsync(Guid userId)
    {
        var userReferral = await _userReferralRepository.GetByUserIdAsync(userId);
        return userReferral?.IsActive ?? false;
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
}

// Helper classes for schema parsing
public class CommissionRuleSchema
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<CommissionRuleLevel> Rules { get; set; } = new();
}

public class CommissionRuleLevel
{
    public int Level { get; set; }
    public decimal Percentage { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? MinAmount { get; set; }
}

public class CommissionLevelConfig
{
    public int Level { get; set; }
    public decimal Percentage { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? MinAmount { get; set; }
}