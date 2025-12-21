using CoreAxis.Modules.DynamicForm.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for Form entity operations.
    /// </summary>
    public interface IFormRepository
    {
        /// <summary>
        /// Gets a form by its identifier.
        /// </summary>
        /// <param name="id">The form identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form if found; otherwise, null.</returns>
        Task<Form> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a form by its identifier including related entities.
        /// </summary>
        /// <param name="id">The form identifier.</param>
        /// <param name="includeFields">Whether to include form fields.</param>
        /// <param name="includeSubmissions">Whether to include form submissions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form if found; otherwise, null.</returns>
        Task<Form> GetByIdWithIncludesAsync(Guid id, bool includeFields = false, bool includeSubmissions = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a form by its name within a tenant.
        /// </summary>
        /// <param name="name">The form name.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form if found; otherwise, null.</returns>
        Task<Form> GetByNameAsync(string name, string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a form by its name within a tenant including related entities.
        /// </summary>
        /// <param name="name">The form name.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="includeFields">Whether to include form fields.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The form if found; otherwise, null.</returns>
        Task<Form> GetByNameWithIncludesAsync(string name, string tenantId, bool includeFields = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all forms for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="includeInactive">Whether to include inactive forms.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of forms.</returns>
        Task<IEnumerable<Form>> GetByTenantAsync(string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all published forms for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of published forms.</returns>
        Task<IEnumerable<Form>> GetPublishedByTenantAsync(string tenantId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets forms with pagination support.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="pageNumber">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="searchTerm">Optional search term to filter forms.</param>
        /// <param name="includeInactive">Whether to include inactive forms.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated result of forms.</returns>
        Task<(IEnumerable<Form> Forms, int TotalCount)> GetPagedAsync(
            string tenantId,
            int pageNumber,
            int pageSize,
            string searchTerm = null,
            bool includeInactive = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets forms by their identifiers.
        /// </summary>
        /// <param name="ids">The form identifiers.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of forms.</returns>
        Task<IEnumerable<Form>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a form with the specified name exists within a tenant.
        /// </summary>
        /// <param name="name">The form name.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="excludeId">Optional form ID to exclude from the check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the form exists; otherwise, false.</returns>
        Task<bool> ExistsAsync(string name, string tenantId, Guid? excludeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of forms for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="includeInactive">Whether to include inactive forms.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of forms.</returns>
        Task<int> GetCountAsync(string tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets forms that have been modified since a specific date.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="since">The date to check modifications since.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of modified forms.</returns>
        Task<IEnumerable<Form>> GetModifiedSinceAsync(string tenantId, DateTime since, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new form to the repository.
        /// </summary>
        /// <param name="form">The form to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(Form form, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing form in the repository.
        /// </summary>
        /// <param name="form">The form to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(Form form, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a form from the repository.
        /// </summary>
        /// <param name="form">The form to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveAsync(Form form, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a form by its identifier.
        /// </summary>
        /// <param name="id">The form identifier.</param>
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