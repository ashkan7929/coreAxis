using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;
using CoreAxis.SharedKernel.Ports;
using CoreAxis.Adapters.Stubs;
using CoreAxis.EventBus;
using CoreAxis.SharedKernel.Contracts;
using Microsoft.Extensions.Logging;
using CoreAxis.Modules.WalletModule.Api;
using CoreAxis.Modules.ProductOrderModule.Api;
using System.Text.Json;
using System.Collections.Generic;
using System;

namespace CoreAxis.Tests.WalletModule.E2ETests;

public class LocalStubProfileTests
{
    private readonly IServiceProvider _serviceProvider;

    public LocalStubProfileTests()
    {
        var services = new ServiceCollection();
        
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Profile"] = "local-stub",
                ["Commission:BaseRate"] = "0.02",
                ["Commission:MinimumFee"] = "1.00",
                ["Commission:MaximumFee"] = "100.00",
                ["PriceProvider:XAU:BasePrice"] = "2000.00",
                ["PriceProvider:BTC:BasePrice"] = "45000.00"
            })
            .Build();
            
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Register stub implementations
        services.AddSingleton<IPriceProvider, InMemoryPriceProvider>();
        services.AddSingleton<IWorkflowClient, WorkflowClientStub>();
        services.AddSingleton<ICommissionEngine, InMemoryCommissionEngine>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        
        // Register wallet module services
        services.AddWalletModuleApi(configuration);
        services.AddProductOrderModuleApi(configuration);
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task LocalStubProfile_ShouldUseStubImplementations()
    {
        // Arrange & Act - Get services from DI container
        using var scope = _serviceProvider.CreateScope();
        var priceProvider = scope.ServiceProvider.GetRequiredService<IPriceProvider>();
        var workflowClient = scope.ServiceProvider.GetRequiredService<IWorkflowClient>();
        var commissionEngine = scope.ServiceProvider.GetRequiredService<ICommissionEngine>();

        // Assert - Verify stub implementations are registered
        Assert.IsType<InMemoryPriceProvider>(priceProvider);
        Assert.IsType<WorkflowClientStub>(workflowClient);
        Assert.IsType<InMemoryCommissionEngine>(commissionEngine);
    }

    [Fact]
    public async Task PriceProvider_ShouldReturnValidQuotes()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var priceProvider = scope.ServiceProvider.GetRequiredService<IPriceProvider>();
        
        var context = new PriceContext(
            tenantId: "test-tenant",
            userId: Guid.NewGuid(),
            correlationId: Guid.NewGuid()
        );

        // Act
        var quote = await priceProvider.GetQuoteAsync("XAU", 1.0m, context);

        // Assert
        Assert.NotNull(quote);
        Assert.Equal("XAU", quote.AssetCode);
        Assert.True(quote.Price > 0);
        Assert.Equal(1.0m, quote.Quantity);
        Assert.True(quote.ExpiresInSeconds > 0);
    }

    [Fact]
    public async Task CommissionEngine_ShouldCalculateCommission()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var commissionEngine = scope.ServiceProvider.GetRequiredService<ICommissionEngine>();
        
        var paymentContext = new PaymentContext(
            userId: Guid.NewGuid(),
            amount: 1000m,
            currency: "USD",
            paymentType: "Purchase",
            tenantId: "test-tenant"
        );

        // Act
        var result = await commissionEngine.CalculateAsync(paymentContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000m, result.OriginalAmount);
        Assert.True(result.CommissionAmount > 0);
        Assert.Equal(result.OriginalAmount - result.CommissionAmount, result.NetAmount);
    }



    [Fact]
    public async Task WorkflowClient_ShouldHandleQuoteWorkflow()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var workflowClient = scope.ServiceProvider.GetRequiredService<IWorkflowClient>();
        
        var context = new Dictionary<string, object>
        {
            ["orderId"] = Guid.NewGuid(),
            ["assetCode"] = "BTC",
            ["quantity"] = 0.1m
        };

        // Act
        var workflowResult = await workflowClient.StartAsync("quote-workflow", context);

        // Assert
        Assert.NotNull(workflowResult);
        Assert.True(workflowResult.IsSuccess);
        Assert.NotEqual(Guid.Empty, workflowResult.WorkflowId);
        
        // Check workflow status
        var status = await workflowClient.GetWorkflowStatusAsync(workflowResult.WorkflowId);
        Assert.Equal(workflowResult.WorkflowId, status.WorkflowId);
    }

    [Fact]
    public void PlaceOrder_ShouldUseStubImplementations()
    {
        // Arrange & Act - Test that stub implementations are registered
        using var scope = _serviceProvider.CreateScope();
        var priceProvider = scope.ServiceProvider.GetService<IPriceProvider>();
        var commissionEngine = scope.ServiceProvider.GetService<ICommissionEngine>();
        
        // Assert
        Assert.NotNull(priceProvider);
        Assert.IsType<InMemoryPriceProvider>(priceProvider);
        Assert.NotNull(commissionEngine);
        Assert.IsType<InMemoryCommissionEngine>(commissionEngine);
    }
}