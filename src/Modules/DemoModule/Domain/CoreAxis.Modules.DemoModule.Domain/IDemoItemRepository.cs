using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DemoModule.Domain
{
    /// <summary>
    /// Repository interface for DemoItem entities.
    /// </summary>
    public interface IDemoItemRepository
    {
        /// <summary>
        /// Gets a demo item by its ID.
        /// </summary>
        /// <param name="id">The ID of the demo item.</param>
        /// <returns>The demo item, or null if not found.</returns>
        Task<DemoItem?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets all demo items.
        /// </summary>
        /// <returns>A collection of demo items.</returns>
        Task<IEnumerable<DemoItem>> GetAllAsync();

        /// <summary>
        /// Gets demo items by category.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <returns>A collection of demo items in the specified category.</returns>
        Task<IEnumerable<DemoItem>> GetByCategoryAsync(string category);

        /// <summary>
        /// Gets featured demo items.
        /// </summary>
        /// <returns>A collection of featured demo items.</returns>
        Task<IEnumerable<DemoItem>> GetFeaturedAsync();

        /// <summary>
        /// Adds a demo item.
        /// </summary>
        /// <param name="demoItem">The demo item to add.</param>
        Task AddAsync(DemoItem demoItem);

        /// <summary>
        /// Updates a demo item.
        /// </summary>
        /// <param name="demoItem">The demo item to update.</param>
        Task UpdateAsync(DemoItem demoItem);

        /// <summary>
        /// Deletes a demo item.
        /// </summary>
        /// <param name="demoItem">The demo item to delete.</param>
        Task DeleteAsync(DemoItem demoItem);
    }
}