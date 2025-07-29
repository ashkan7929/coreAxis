using CoreAxis.Modules.MLMModule.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.MLMModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMLMModuleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Add DbContext when ready
        // services.AddDbContext<MLMDbContext>(options =>
        //     options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // TODO: Register repositories when implemented
        // services.AddScoped<IUserReferralRepository, UserReferralRepository>();
        // services.AddScoped<ICommissionTransactionRepository, CommissionTransactionRepository>();
        // services.AddScoped<ICommissionRuleSetRepository, CommissionRuleSetRepository>();

        return services;
    }
}