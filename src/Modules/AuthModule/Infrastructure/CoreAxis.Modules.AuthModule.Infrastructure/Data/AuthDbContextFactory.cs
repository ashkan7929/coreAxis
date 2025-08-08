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
        var connectionString = "Server=62.3.41.64;Database=CoreAxisDb;User id=BMSUser;Encrypt=True;TrustServerCertificate=True;Password=pxi[KaBKH]l2T@tsWF?!:l92l%|?$5";

        optionsBuilder.UseSqlServer(connectionString);

        return new AuthDbContext(optionsBuilder.Options);
    }
}