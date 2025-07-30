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
        var connectionString = "Server=.\\SQLEXPRESS;Database=CoreAxisDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";
        
        optionsBuilder.UseSqlServer(connectionString);

        return new AuthDbContext(optionsBuilder.Options);
    }
}