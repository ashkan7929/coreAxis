using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.Modules.AuthModule.Infrastructure.Data;
using CoreAxis.Modules.AuthModule.Infrastructure.Repositories;
using CoreAxis.Modules.AuthModule.Infrastructure.Services;
using CoreAxis.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Hosting;

namespace CoreAxis.Modules.AuthModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthModuleInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        services.AddDbContext<AuthDbContext>(options =>
        {
            var useInMemory = configuration.GetValue<bool>("Auth:UseInMemoryDb");
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (env.IsDevelopment() && useInMemory)
            {
                options.UseInMemoryDatabase("CoreAxisAuthDb");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required");
                }
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    sql.CommandTimeout(60);
                });
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IActionRepository, ActionRepository>();
        services.AddScoped<IAccessLogRepository, AccessLogRepository>();
        services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IShahkarService, ShahkarService>();
        services.AddScoped<ICivilRegistryService, CivilRegistryService>();
        services.AddScoped<IMegfaSmsService, MegfaSmsService>();
        services.AddScoped<IAuthDataSeeder, AuthDataSeeder>();

        services.AddHttpClient<IShahkarService, ShahkarService>();
        services.AddHttpClient<ICivilRegistryService, CivilRegistryService>();

        services.AddHttpContextAccessor();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}