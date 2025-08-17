using CoreAxis.Modules.DynamicForm.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for FormulaDefinition entity operations.
    /// </summary>
    public interface IFormulaDefinitionRepository
    {
        /// <summary>
        /// Gets a formula definition by its identifier.
        /// </summary>
        /// <param name="id">The formula definition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The formula definition if found; otherwise, null.</returns>
        Task<FormulaDefinition> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a formula definition by its identifier including evaluation logs.
        /// </summary>
        /// <param name="id">The formula definition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The formula definition if found; otherwise, null.</returns>
        Task<FormulaDefinition> GetByIdWithLogsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a formula definition by its name within a tenant.
        /// </summary>
        /// <param name="name">The formula name.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The formula definition if found; otherwise, null.</returns>
        Task<FormulaDefinition> GetByNameAsync(string name, string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all formula definitions for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="includeInactive">Whether to include inactive formulas.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of formula definitions.</returns>
        Task<IEnumerable<FormulaDefinition>> GetByTenantAsync(string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all published formula definitions for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of published formula definitions.</returns>
        Task<IEnumerable<FormulaDefinition>> GetPublishedByTenantAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets formula definitions by category.
        /// </summary>
        /// <param name="category">The formula category.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="includeInactive">Whether to include inactive formulas.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of formula definitions.</returns>
        Task<IEnumerable<FormulaDefinition>> GetByCategoryAsync(string category, string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets formula definitions by return type.
        /// </summary>
        /// <param name="returnType">The return type.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="includeInactive">Whether to include inactive formulas.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of formula definitions.</returns>
        Task<IEnumerable<FormulaDefinition>> GetByReturnTypeAsync(string returnType, string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets formula definitions by tags.
        /// </summary>
        /// <param name="tags">The tags to search for.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="includeInactive">Whether to include inactive formulas.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of formula definitions.</returns>
        Task<IEnumerable<FormulaDefinition>> GetByTagsAsync(IEnumerable<string> tags, string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets formula definitions with pagination support.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="searchTerm">Optional search term to filter formulas.</param>
        /// <param name="category">Optional category filter.</param>
        /// <param name="returnType">Optional return type filter.</param>
        /// <param name="isPublished">Optional published status filter.</param>
        /// <param name="includeInactive">Whether to include inactive formulas.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated result of formula definitions.</returns>
        Task<(IEnumerable<FormulaDefinition> Formulas, int TotalCount)> GetPagedAsync(
            string tenantId,
            int pageNumber,
            int pageSize,
            string searchTerm = null,
            string category = null,
            string returnType = null,
            bool? isPublished = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets formula definitions by their identifiers.
        /// </summary>
        /// <param name="ids">The formula definition identifiers.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of formula definitions.</returns>
        Task<IEnumerable<FormulaDefinition>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets deprecated formula definitions.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of deprecated formula definitions.</returns>
        Task<IEnumerable<FormulaDefinition>> GetDeprecatedAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets formula definitions that depend on a specific formula.
        /// </summary>
        /// <param name="formulaId">The formula identifier to check dependencies for.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of formula definitions that depend on the specified formula.</returns>
        Task<IEnumerable<FormulaDefinition>> GetDependentFormulasAsync(Guid formulaId, string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available categories for a tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of distinct categories.</returns>
        Task<IEnumerable<string>> GetCategoriesAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available tags for a tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of distinct tags.</returns>
        Task<IEnumerable<string>> GetTagsAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a formula definition with the specified name exists within a tenant.
        /// </summary>
        /// <param name="name">The formula name.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="excludeId">Optional formula ID to exclude from the check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the formula exists; otherwise, false.</returns>
        Task<bool> ExistsAsync(string name, string tenantId, Guid? excludeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of formula definitions for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="includeInactive">Whether to include inactive formulas.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of formula definitions.</returns>
        Task<int> GetCountAsync(string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets formula definitions that have been modified since a specific date.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="since">The date to check modifications since.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of modified formula definitions.</returns>
        Task<IEnumerable<FormulaDefinition>> GetModifiedSinceAsync(string tenantId, DateTime since, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches formula definitions by expression content.
        /// </summary>
        /// <param name="expressionPattern">The expression pattern to search for.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of formula definitions containing the expression pattern.</returns>
        Task<IEnumerable<FormulaDefinition>> SearchByExpressionAsync(string expressionPattern, string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets formula usage statistics.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="fromDate">Optional start date for statistics.</param>
        /// <param name="toDate">Optional end date for statistics.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A dictionary containing usage statistics by formula ID.</returns>
        Task<Dictionary<Guid, int>> GetUsageStatisticsAsync(string tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new formula definition to the repository.
        /// </summary>
        /// <param name="formulaDefinition">The formula definition to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(FormulaDefinition formulaDefinition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing formula definition in the repository.
        /// </summary>
        /// <param name="formulaDefinition">The formula definition to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(FormulaDefinition formulaDefinition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a formula definition from the repository.
        /// </summary>
        /// <param name="formulaDefinition">The formula definition to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveAsync(FormulaDefinition formulaDefinition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a formula definition by its identifier.
        /// </summary>
        /// <param name="id">The formula definition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all pending changes to the repository.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of affected records.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}