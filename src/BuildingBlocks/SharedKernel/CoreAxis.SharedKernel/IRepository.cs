using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CoreAxis.SharedKernel
{
    /// <summary>
    /// Base repository interface for all entities.
    /// Provides common CRUD operations and query capabilities.
    /// </summary>
    /// <typeparam name="T">The entity type that inherits from EntityBase</typeparam>
    public interface IRepository<T> where T : EntityBase
    {
        /// <summary>
        /// Gets an entity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier</param>
        /// <returns>The entity if found, null otherwise</returns>
        Task<T?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets all entities.
        /// </summary>
        /// <returns>A queryable collection of all entities</returns>
        IQueryable<T> GetAll();

        /// <summary>
        /// Finds entities based on a predicate.
        /// </summary>
        /// <param name="predicate">The search predicate</param>
        /// <returns>A queryable collection of matching entities</returns>
        IQueryable<T> Find(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adds a new entity.
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task AddAsync(T entity);

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        void Update(T entity);

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        void Delete(T entity);

        /// <summary>
        /// Deletes an entity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Checks if an entity exists with the given identifier.
        /// </summary>
        /// <param name="id">The unique identifier</param>
        /// <returns>True if the entity exists, false otherwise</returns>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// Gets the count of entities matching the predicate.
        /// </summary>
        /// <param name="predicate">The search predicate (optional)</param>
        /// <returns>The count of matching entities</returns>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    }
}