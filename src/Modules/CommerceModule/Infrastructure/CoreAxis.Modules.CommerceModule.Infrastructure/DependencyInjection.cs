using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Infrastructure.Data;
using CoreAxis.Modules.CommerceModule.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.CommerceModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommerceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<CommerceDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            options.UseSqlServer(connectionString, b =>
            {
                b.MigrationsAssembly(typeof(CommerceDbContext).Assembly.FullName);
                b.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }

            options.EnableDetailedErrors();
        });

        // Register repositories
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionPaymentRepository, SubscriptionPaymentRepository>();
        services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
        services.AddScoped<IReconciliationEntryRepository, ReconciliationEntryRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();

        // Add health checks
        services.AddHealthChecks()
            .AddDbContextCheck<CommerceDbContext>("commerce-db");

        return services;
    }

    public static async Task<IServiceProvider> MigrateCommerceDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CommerceDbContext>>();

        try
        {
            logger.LogInformation("Starting CommerceModule database migration...");
            await context.Database.MigrateAsync();
            logger.LogInformation("CommerceModule database migration completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during CommerceModule database migration");
            throw;
        }

        return serviceProvider;
    }

    public static async Task<IServiceProvider> SeedCommerceData(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CommerceDbContext>>();

        try
        {
            logger.LogInformation("Starting CommerceModule data seeding...");
            
            // Add any seed data here if needed
            // Example: Default discount rules, system configurations, etc.
            
            await context.SaveChangesAsync();
            logger.LogInformation("CommerceModule data seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during CommerceModule data seeding");
            throw;
        }

        return serviceProvider;
    }
}