using CoreAxis.Modules.MLMModule.Application.DTOs;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.MLMModule.Application.Services;

public interface ICommissionRuleSetService
{
    /// <summary>
    /// Creates a new commission rule set
    /// </summary>
    /// <param name="dto">Commission rule set data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created commission rule set</returns>
    Task<Result<CommissionRuleDto>> CreateRuleSetAsync(
        CreateCommissionRuleSetDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing commission rule set
    /// </summary>
    /// <param name="id">Rule set ID</param>
    /// <param name="dto">Updated commission rule set data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated commission rule set</returns>
    Task<Result<CommissionRuleDto>> UpdateRuleSetAsync(
        Guid id,
        UpdateCommissionRuleSetDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a commission rule set by ID
    /// </summary>
    /// <param name="id">Rule set ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Commission rule set</returns>
    Task<Result<CommissionRuleDto>> GetRuleSetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all commission rule sets with optional filtering
    /// </summary>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of commission rule sets</returns>
    Task<Result<IEnumerable<CommissionRuleDto>>> GetAllRuleSetsAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default commission rule set
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Default commission rule set</returns>
    Task<Result<CommissionRuleDto>> GetDefaultRuleSetAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets commission rule set by product ID
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Commission rule set for the product</returns>
    Task<Result<CommissionRuleDto>> GetRuleSetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a commission rule set
    /// </summary>
    /// <param name="id">Rule set ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> ActivateRuleSetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a commission rule set
    /// </summary>
    /// <param name="id">Rule set ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> DeactivateRuleSetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a commission rule set as default
    /// </summary>
    /// <param name="id">Rule set ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> SetAsDefaultAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a commission rule set
    /// </summary>
    /// <param name="id">Rule set ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> DeleteRuleSetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a new version of the commission rule set (immutable)
    /// </summary>
    /// <param name="ruleSetId">Rule set ID</param>
    /// <param name="schemaJson">Schema JSON for the version</param>
    /// <param name="publishedBy">User who published the version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Published version</returns>
    Task<Result<CommissionRuleVersionDto>> PublishVersionAsync(
        Guid ruleSetId,
        string schemaJson,
        string publishedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a commission rule set
    /// </summary>
    /// <param name="ruleSetId">Rule set ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of versions</returns>
    Task<Result<IEnumerable<CommissionRuleVersionDto>>> GetVersionsAsync(
        Guid ruleSetId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates commission rule set schema JSON
    /// </summary>
    /// <param name="schemaJson">Schema JSON to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<Result<bool>> ValidateSchemaAsync(
        string schemaJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a product rule binding to a commission rule set
    /// </summary>
    /// <param name="ruleSetId">Rule set ID</param>
    /// <param name="dto">Product rule binding data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product rule binding</returns>
    Task<Result<ProductRuleBindingDto>> AddProductBindingAsync(
        Guid ruleSetId,
        CreateProductRuleBindingDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a product rule binding from a commission rule set
    /// </summary>
    /// <param name="ruleSetId">Rule set ID</param>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> RemoveProductBindingAsync(
        Guid ruleSetId,
        Guid productId,
        CancellationToken cancellationToken = default);
}