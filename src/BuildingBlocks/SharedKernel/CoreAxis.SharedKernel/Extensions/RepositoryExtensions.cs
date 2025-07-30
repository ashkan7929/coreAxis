using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.SharedKernel.Extensions
{
    /// <summary>
    /// Extension methods for IRepository interface.
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Gets all entities asynchronously.
        /// </summary>
        /// <typeparam name="T">The entity type that inherits from EntityBase</typeparam>
        /// <param name="repository">The repository</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of all entities</returns>
        public static async Task<IEnumerable<T>> GetAllAsync<T>(this IRepository<T> repository, CancellationToken cancellationToken = default) where T : EntityBase
        {
            return await repository.GetAll().ToListAsync(cancellationToken);
        }
    }
}