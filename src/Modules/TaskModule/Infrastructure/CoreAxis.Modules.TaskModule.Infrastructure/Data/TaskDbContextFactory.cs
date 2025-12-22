using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreAxis.Modules.TaskModule.Infrastructure.Data;

public class TaskDbContextFactory : IDesignTimeDbContextFactory<TaskDbContext>
{
    public TaskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TaskDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("COREAXIS_CONNECTION_STRING environment variable is not set.");

        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "task");
            sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
        });

        return new TaskDbContext(optionsBuilder.Options, new NoOpDomainEventDispatcher());
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : DomainEvent
            => Task.CompletedTask;

        public Task DispatchAsync(IEnumerable<DomainEvent> domainEvents)
            => Task.CompletedTask;
    }
}
