using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Context;
using CoreAxis.SharedKernel.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreAxis.Modules.Workflow.Infrastructure.Data;

public class WorkflowDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WorkflowDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("COREAXIS_CONNECTION_STRING environment variable is not set.");

        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "workflow");
            sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
        });

        return new WorkflowDbContext(optionsBuilder.Options, new NoOpDomainEventDispatcher(), new NoOpTenantProvider());
    }

    private sealed class NoOpTenantProvider : ITenantProvider
    {
        public string? TenantId => "default";
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : DomainEvent
            => Task.CompletedTask;

        public Task DispatchAsync(IEnumerable<DomainEvent> domainEvents)
            => Task.CompletedTask;
    }
}