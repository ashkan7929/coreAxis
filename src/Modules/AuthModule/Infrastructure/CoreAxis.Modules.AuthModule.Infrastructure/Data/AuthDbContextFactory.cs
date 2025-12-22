using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Data;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();

        // Use SQL Server Express with the same database name as appsettings.json
        var connectionString = Environment.GetEnvironmentVariable("COREAXIS_CONNECTION_STRING")
            ?? throw new InvalidOperationException("COREAXIS_CONNECTION_STRING environment variable is not set.");

        optionsBuilder.UseSqlServer(connectionString);

        return new AuthDbContext(optionsBuilder.Options);
    }
}