using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CoreAxis.SharedKernel
{
    /// <summary>
    /// Base repository implementation for all entities.
    /// Provides common CRUD operations and query capabilities.
    /// </summary>
    /// <typeparam name="T">The entity type that inherits from EntityBase</typeparam>
    public class Repository<T> : IRepository<T> where T : EntityBase
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        /// <summary>
        /// Gets an entity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier</param>
        /// <returns>The entity if found, null otherwise</returns>
        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// Gets all entities.
        /// </summary>
        /// <returns>A queryable collection of all entities</returns>
        public virtual IQueryable<T> GetAll()
        {
            return _dbSet.AsQueryable();
        }

        /// <summary>
        /// Finds entities based on a predicate.
        /// </summary>
        /// <param name="predicate">The search predicate</param>
        /// <returns>A queryable collection of matching entities</returns>
        public virtual IQueryable<T> Find(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate);
        }

        /// <summary>
        /// Adds a new entity.
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        public virtual void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        /// <summary>
        /// Deletes an entity by its identifier.
        /// </summary>
        /// <param name="id">The unique identifier</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public virtual async Task DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                Delete(entity);
            }
        }

        /// <summary>
        /// Checks if an entity exists with the given identifier.
        /// </summary>
        /// <param name="id">The unique identifier</param>
        /// <returns>True if the entity exists, false otherwise</returns>
        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.AnyAsync(e => e.Id == id);
        }

        /// <summary>
        /// Gets the count of entities matching the predicate.
        /// </summary>
        /// <param name="predicate">The search predicate</param>
        /// <returns>The count of matching entities</returns>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();
            
            return await _dbSet.CountAsync(predicate);
        }
    }
}