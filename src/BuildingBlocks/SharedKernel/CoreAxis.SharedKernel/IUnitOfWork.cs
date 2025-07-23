using System;
using System.Threading.Tasks;

namespace CoreAxis.SharedKernel
{
    /// <summary>
    /// Unit of Work pattern interface for managing database transactions.
    /// Provides a way to group multiple repository operations into a single transaction.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Saves all changes made in this unit of work to the database.
        /// </summary>
        /// <returns>The number of state entries written to the database</returns>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commits the current database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rolls back the current database transaction.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        Task RollbackTransactionAsync();
    }
}