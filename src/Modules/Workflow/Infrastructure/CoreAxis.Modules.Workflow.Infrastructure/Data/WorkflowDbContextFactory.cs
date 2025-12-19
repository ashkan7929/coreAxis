using CoreAxis.SharedKernel.Domain;
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
            ?? "Server=(localdb)\\mssqllocaldb;Database=CoreAxis_Workflow;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "workflow");
            sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
        });

        return new WorkflowDbContext(optionsBuilder.Options, new NoOpDomainEventDispatcher());
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : DomainEvent
            => Task.CompletedTask;

        public Task DispatchAsync(IEnumerable<DomainEvent> domainEvents)
            => Task.CompletedTask;
    }
}