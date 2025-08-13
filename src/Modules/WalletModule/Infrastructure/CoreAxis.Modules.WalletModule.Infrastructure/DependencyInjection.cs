using CoreAxis.Modules.WalletModule.Application.Services;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using CoreAxis.Modules.WalletModule.Infrastructure.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.WalletModule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWalletModuleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<WalletDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Register repositories
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWalletTypeRepository, WalletTypeRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionTypeRepository, TransactionTypeRepository>();
        services.AddScoped<IWalletProviderRepository, WalletProviderRepository>();
        services.AddScoped<IWalletContractRepository, WalletContractRepository>();

        // Register services
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}