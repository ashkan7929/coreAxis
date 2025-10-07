using CoreAxis.Modules.WalletModule.Application.Services;
using CoreAxis.Modules.WalletModule.Domain.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Data;
using CoreAxis.Modules.WalletModule.Infrastructure.Repositories;
using CoreAxis.Modules.WalletModule.Infrastructure.Services;
using CoreAxis.Modules.WalletModule.Infrastructure.Configuration;
using CoreAxis.Modules.WalletModule.Infrastructure.Providers;
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

        // Options: Wallet policy configuration (AllowNegative, DailyDebitCap)
        services.AddOptions<WalletPolicyOptions>()
            .Bind(configuration.GetSection("Wallet:Policy"));

        // Register services
        services.AddScoped<IWalletPolicyService, WalletPolicyService>();
        services.AddScoped<ITransactionService, TransactionService>();

        // Hosted services
        services.AddHostedService<CommissionSettlementHostedService>();
        services.AddHostedService<BalanceSnapshotBackgroundService>();

        // Providers
        services.AddSingleton<IBalanceSnapshotProvider, InMemoryBalanceSnapshotProvider>();

        return services;
    }
}