using System;
using System.Threading.Tasks;

namespace CoreAxis.SharedKernel
{
    /// <summary>
    /// Unit of Work interface for managing transactional boundaries per module DbContext.
    /// Coordinates committing domain changes and Outbox messages atomically.
    /// </summary>
    /// <remarks>
    /// Guidance:
    /// - Treat each module's DbContext as the transactional boundary.
    /// - Persist domain aggregates via repositories, append Outbox messages, then commit once.
    /// - Outbox messages are emitted by <see cref="Outbox.OutboxPublisher"/> after commit.
    ///
    /// Example:
    /// <code>
    /// public async Task PlaceOrderAsync(Order order, IList&lt;IntegrationEvent&gt; events, CancellationToken ct)
    /// {
    ///     await _ordersRepository.AddAsync(order);
    ///     foreach (var evt in events)
    ///     {
    ///         var msg = new Outbox.OutboxMessage(evt.GetType().AssemblyQualifiedName!,
    ///             System.Text.Json.JsonSerializer.Serialize(evt), evt.CorrelationId, evt.CausationId, order.TenantId);
    ///         await _outboxService.AddMessageAsync(msg, ct);
    ///     }
    ///
    ///     await _unitOfWork.SaveChangesAsync(); // commits aggregates + outbox atomically
    /// }
    /// </code>
    /// </remarks>
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