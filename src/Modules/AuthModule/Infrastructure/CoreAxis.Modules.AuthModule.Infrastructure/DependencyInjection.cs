using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using CoreAxis.Modules.AuthModule.Infrastructure.Repositories;
using CoreAxis.Modules.AuthModule.Infrastructure.Services;
using CoreAxis.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.AuthModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthModuleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext - Use SQL Server database
        services.AddDbContext<AuthDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                // Use in-memory database only if no connection string is provided
                options.UseInMemoryDatabase("CoreAxisAuthDb");
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        // Add Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IActionRepository, ActionRepository>();
        services.AddScoped<IAccessLogRepository, AccessLogRepository>();

        // Add Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}