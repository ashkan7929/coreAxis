using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Data;

public class DynamicFormDbContextFactory : IDesignTimeDbContextFactory<DynamicFormDbContext>
{
    public DynamicFormDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DynamicFormDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("COREAXIS_CONNECTION_STRING environment variable is not set.");

        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "dynamicform");
            sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
        });

        return new DynamicFormDbContext(optionsBuilder.Options);
    }
}