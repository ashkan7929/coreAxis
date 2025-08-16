using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Events;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Repositories;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.EventHandlers;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Services;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Outbox;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.EventBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductOrderModuleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add SharedKernel services
        services.AddSharedKernel();
        
        // Add DbContext
        services.AddDbContext<ProductOrderDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        
        // Register domain event handlers
        services.AddScoped<IDomainEventHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();
        
        // Register integration event handlers
        services.AddScoped<IIntegrationEventHandler<OrderPlaced>, OrderPlacedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<PriceLocked>, PriceLockedIntegrationEventHandler>();
        
        // Register workflow integration service
        services.AddScoped<IWorkflowIntegrationService, WorkflowIntegrationService>();
        
        return services;
    }
}