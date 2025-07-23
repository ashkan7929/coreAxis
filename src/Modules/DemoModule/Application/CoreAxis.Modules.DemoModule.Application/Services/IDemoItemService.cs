using CoreAxis.SharedKernel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DemoModule.Application.Services
{
    /// <summary>
    /// Interface for the demo item service.
    /// </summary>
    public interface IDemoItemService
    {
        /// <summary>
        /// Gets a demo item by its ID.
        /// </summary>
        /// <param name="id">The ID of the demo item.</param>
        /// <returns>The result containing the demo item, or an error if not found.</returns>
        Task<Result<Domain.DemoItem>> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets all demo items with pagination.
        /// </summary>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A paginated list of demo items.</returns>
        Task<PaginatedList<Domain.DemoItem>> GetAllAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Gets demo items by category with pagination.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A paginated list of demo items in the specified category.</returns>
        Task<PaginatedList<Domain.DemoItem>> GetByCategoryAsync(string category, int pageNumber, int pageSize);

        /// <summary>
        /// Gets featured demo items.
        /// </summary>
        /// <returns>A collection of featured demo items.</returns>
        Task<IEnumerable<Domain.DemoItem>> GetFeaturedAsync();

        /// <summary>
        /// Creates a new demo item.
        /// </summary>
        /// <param name="name">The name of the demo item.</param>
        /// <param name="description">The description of the demo item.</param>
        /// <param name="price">The price of the demo item.</param>
        /// <param name="category">The category of the demo item.</param>
        /// <returns>The result containing the created demo item, or an error if creation failed.</returns>
        Task<Result<Domain.DemoItem>> CreateAsync(string name, string description, decimal price, string category);

        /// <summary>
        /// Updates a demo item.
        /// </summary>
        /// <param name="id">The ID of the demo item to update.</param>
        /// <param name="name">The new name.</param>
        /// <param name="description">The new description.</param>
        /// <param name="price">The new price.</param>
        /// <param name="category">The new category.</param>
        /// <returns>The result containing the updated demo item, or an error if update failed.</returns>
        Task<Result<Domain.DemoItem>> UpdateAsync(Guid id, string name, string description, decimal price, string category);

        /// <summary>
        /// Sets whether a demo item is featured.
        /// </summary>
        /// <param name="id">The ID of the demo item.</param>
        /// <param name="isFeatured">Whether the demo item is featured.</param>
        /// <returns>The result containing the updated demo item, or an error if update failed.</returns>
        Task<Result<Domain.DemoItem>> SetFeaturedAsync(Guid id, bool isFeatured);

        /// <summary>
        /// Deletes a demo item.
        /// </summary>
        /// <param name="id">The ID of the demo item to delete.</param>
        /// <returns>The result indicating success or failure.</returns>
        Task<Result<bool>> DeleteAsync(Guid id);
    }
}