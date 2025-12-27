using CoreAxis.Modules.MLMModule.Application.Contracts;
using CoreAxis.EventBus;
using CoreAxis.Modules.MLMModule.Application.Services;
using CoreAxis.Modules.MLMModule.Domain.Events;
using CoreAxis.Modules.MLMModule.Domain.Repositories;
using CoreAxis.Modules.MLMModule.Infrastructure.Data;
using CoreAxis.Modules.MLMModule.Infrastructure.EventHandlers;
using CoreAxis.Modules.MLMModule.Infrastructure.Background;
using CoreAxis.Modules.MLMModule.Infrastructure.Repositories;
using CoreAxis.Modules.MLMModule.Infrastructure.Services;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CoreAxis.Modules.MLMModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMLMModuleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<MLMModuleDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                sql.CommandTimeout(60);
            }));

        // Register repositories
        services.AddScoped<IUserReferralRepository, UserReferralRepository>();
        services.AddScoped<ICommissionTransactionRepository, CommissionTransactionRepository>();
        services.AddScoped<ICommissionRuleSetRepository, CommissionRuleSetRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IMLMUnitOfWork, UnitOfWork>();

        // Register services
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<ICommissionManagementService, CommissionManagementService>();

        // Register event handlers
        services.AddScoped<IIntegrationEventHandler<PaymentConfirmed>, PaymentConfirmedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<OrderFinalized>, OrderFinalizedIntegrationEventHandler>();
        services.AddScoped<IDomainEventHandler<CommissionApprovedEvent>, CommissionApprovedEventHandler>();

        // Register background hosted services
        services.AddHostedService<CommissionProcessingHostedService>();
        services.AddHostedService<CommissionSettlementHostedService>();

        return services;
    }
}