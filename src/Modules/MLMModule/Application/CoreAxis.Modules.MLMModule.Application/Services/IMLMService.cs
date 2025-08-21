using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.Modules.MLMModule.Application.Contracts;
using CoreAxis.Modules.MLMModule.Domain.Enums;

namespace CoreAxis.Modules.MLMModule.Application.Services;

public interface IMLMService
{
    /// <summary>
    /// Processes commission calculations for a payment
    /// </summary>
    /// <param name="sourcePaymentId">The payment that triggered the commission</param>
    /// <param name="productId">The product associated with the payment</param>
    /// <param name="amount">The payment amount</param>
    /// <param name="buyerUserId">The user who made the purchase</param>
    /// <returns>List of created commission transactions</returns>
    Task<IEnumerable<CommissionTransactionDto>> ProcessCommissionAsync(
        Guid sourcePaymentId,
        Guid productId,
        decimal amount,
        Guid buyerUserId);

    /// <summary>
    /// Calculates potential commissions for a given amount and user
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="amount">The amount to calculate commissions for</param>
    /// <param name="buyerUserId">The user who would make the purchase</param>
    /// <returns>List of potential commission calculations</returns>
    Task<IEnumerable<CommissionCalculationDto>> CalculatePotentialCommissionsAsync(
        Guid productId,
        decimal amount,
        Guid buyerUserId);

    /// <summary>
    /// Builds the MLM network tree for a user
    /// </summary>
    /// <param name="userId">The root user ID</param>
    /// <param name="maxDepth">Maximum depth to traverse</param>
    /// <returns>Network tree structure</returns>
    Task<NetworkTreeNodeDto> BuildNetworkTreeAsync(
        Guid userId,
        int maxDepth = 10);

    /// <summary>
    /// Gets network statistics for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Network statistics</returns>
    Task<MLMNetworkStatsDto> GetNetworkStatsAsync(
        Guid userId);

    /// <summary>
    /// Validates if a user can be added as a referral under a parent
    /// </summary>
    /// <param name="parentUserId">The parent user ID</param>
    /// <param name="childUserId">The child user ID</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateReferralRelationshipAsync(
        Guid parentUserId,
        Guid childUserId);

    /// <summary>
    /// Gets the upline chain for a user up to a specified level
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="maxLevels">Maximum levels to traverse</param>
    /// <returns>List of upline users</returns>
    Task<IEnumerable<UserReferralDto>> GetUplineChainAsync(
        Guid userId,
        int maxLevels = 10);

    /// <summary>
    /// Processes expired commissions
    /// </summary>
    /// <param name="expirationDate">The expiration date</param>
    /// <returns>Number of commissions processed</returns>
    Task<int> ProcessExpiredCommissionsAsync(
        DateTime expirationDate);

    /// <summary>
    /// Gets the downline users for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="maxDepth">Maximum depth to traverse</param>
    /// <returns>List of downline users</returns>
    Task<IEnumerable<UserReferralDto>> GetDownlineAsync(
        Guid userId,
        int maxDepth = 10);

    /// <summary>
    /// Gets the downline users for a specific user with pagination
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="request">Downline request with pagination</param>
    /// <returns>Paginated list of downline users</returns>
    Task<IEnumerable<UserReferralDto>> GetDownlineAsync(
        Guid userId,
        GetDownlineRequest request);

    /// <summary>
    /// Gets referral information for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>User referral information</returns>
    Task<UserReferralDto?> GetUserReferralInfoAsync(
        Guid userId);

    /// <summary>
    /// Joins a user to the MLM network
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="request">Join MLM request</param>
    /// <returns>User referral information</returns>
    Task<UserReferralDto> JoinMLMAsync(
        Guid userId,
        object request);
}

public class CommissionCalculationDto
{
    public Guid UserId { get; set; }
    public int Level { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public decimal SourceAmount { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
}