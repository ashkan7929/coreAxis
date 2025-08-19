using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.Modules.MLMModule.Infrastructure.Data;
using CoreAxis.Modules.MLMModule.Infrastructure.EventHandlers;
using CoreAxis.Modules.MLMModule.Infrastructure.Repositories;
using CoreAxis.Modules.MLMModule.Infrastructure.Services;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.MLMModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMLMModuleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<MLMDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Register repositories
        services.AddScoped<IUserReferralRepository, UserReferralRepository>();
        services.AddScoped<ICommissionTransactionRepository, CommissionTransactionRepository>();
        services.AddScoped<ICommissionRuleSetRepository, CommissionRuleSetRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // Register services
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        // Register event handlers
        services.AddScoped<PaymentConfirmedEventHandler>();

        return services;
    }
}