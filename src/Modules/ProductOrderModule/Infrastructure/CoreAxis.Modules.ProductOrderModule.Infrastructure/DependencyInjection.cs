using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Events;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Repositories;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.EventHandlers;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Services;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Adapters;
using CoreAxis.SharedKernel.Ports;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.SharedKernel.Outbox;
using CoreAxis.SharedKernel.Contracts.Events;
using CoreAxis.EventBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CoreAxis.Modules.ProductOrderModule.Domain.Products;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using CoreAxis.Modules.ProductOrderModule.Application.Services;
using CoreAxis.Modules.ProductOrderModule.Domain.Suppliers;
using CoreAxis.Modules.ProductOrderModule.Domain.Quotes;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Connectors;
using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Connectors;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Options;
using Microsoft.Extensions.Configuration;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductOrderModuleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add SharedKernel services
        services.AddSharedKernel();

        // Configure Options
        services.Configure<FanavaranOptions>(configuration.GetSection("Fanavaran"));
        
        // Add DbContext
        services.AddDbContext<ProductOrderDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                sql.CommandTimeout(60);
            }));

        // Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        
        // Register connectors
        services.AddHttpClient<IFanavaranConnector, FanavaranConnector>();
        services.AddHttpClient<IRiskConnector, RiskConnector>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        
        // Register domain event handlers
        services.AddScoped<IDomainEventHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();
        
        // Register integration event handlers
        services.AddScoped<IIntegrationEventHandler<OrderPlaced>, OrderPlacedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<PriceLocked>, PriceLockedIntegrationEventHandler>();
        
        // Register workflow integration service
        services.AddScoped<IWorkflowIntegrationService, WorkflowIntegrationService>();

        // Register price provider (default: ApiManager adapter)
        services.AddScoped<IPriceProvider, PriceProviderViaApiManager>();
        services.AddScoped<IProductEventEmitter, ProductEventEmitter>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddOptions<ProductEventsOptions>();
        
        return services;
    }
}