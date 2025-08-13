using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using CoreAxis.Modules.WalletModule.Api;
using CoreAxis.ApiGateway;
using CoreAxis.SharedKernel.Ports;
using CoreAxis.Adapters.Stubs;
using CoreAxis.EventBus;
using CoreAxis.SharedKernel.Contracts;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Tests.WalletModule.E2ETests;

public class LocalStubProfileTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LocalStubProfileTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Profile"] = "local-stub",
                    ["Commission:BaseRate"] = "0.02",
                    ["Commission:MinimumFee"] = "1.00",
                    ["Commission:MaximumFee"] = "100.00",
                    ["PriceProvider:XAU:BasePrice"] = "2000.00",
                    ["PriceProvider:BTC:BasePrice"] = "45000.00"
                });
            });
        });
        
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task LocalStubProfile_ShouldUseStubImplementations()
    {
        // Arrange & Act - Get services from DI container
        using var scope = _factory.Services.CreateScope();
        var priceProvider = scope.ServiceProvider.GetRequiredService<IPriceProvider>();
        var workflowClient = scope.ServiceProvider.GetRequiredService<IWorkflowClient>();
        var commissionEngine = scope.ServiceProvider.GetRequiredService<ICommissionEngine>();
        var paymentGateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
        var notificationClient = scope.ServiceProvider.GetRequiredService<INotificationClient>();

        // Assert - Verify stub implementations are registered
        Assert.IsType<InMemoryPriceProvider>(priceProvider);
        Assert.IsType<WorkflowClientStub>(workflowClient);
        Assert.IsType<InMemoryCommissionEngine>(commissionEngine);
        Assert.IsType<InMemoryPaymentGateway>(paymentGateway);
        Assert.IsType<InMemoryNotificationClient>(notificationClient);
    }

    [Fact]
    public async Task PriceProvider_ShouldReturnValidQuotes()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var priceProvider = scope.ServiceProvider.GetRequiredService<IPriceProvider>();
        
        var context = new PriceContext(
            assetCode: "XAU",
            quantity: 1.0m,
            currency: "USD",
            tenantId: "test-tenant"
        );

        // Act
        var quote = await priceProvider.GetPriceQuoteAsync(context);

        // Assert
        Assert.NotNull(quote);
        Assert.Equal("XAU", quote.AssetCode);
        Assert.True(quote.Price > 0);
        Assert.Equal("USD", quote.Currency);
        Assert.True(quote.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task CommissionEngine_ShouldCalculateCommission()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
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
    public async Task PaymentGateway_ShouldProcessPayments()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var paymentGateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
        
        var request = new PaymentRequest(
            amount: 100m,
            currency: "USD",
            paymentMethod: "CreditCard",
            userId: Guid.NewGuid(),
            idempotencyKey: Guid.NewGuid().ToString()
        );

        // Act
        var result = await paymentGateway.ChargeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ReferenceId);
        Assert.Equal(100m, result.Amount);
        Assert.Equal("USD", result.Currency);
        
        // Verify payment if successful
        if (result.IsSuccess)
        {
            var verification = await paymentGateway.VerifyAsync(result.ReferenceId);
            Assert.True(verification.IsVerified);
            Assert.Equal(result.ReferenceId, verification.ReferenceId);
        }
    }

    [Fact]
    public async Task NotificationClient_ShouldSendNotifications()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var notificationClient = scope.ServiceProvider.GetRequiredService<INotificationClient>();
        
        var request = new NotificationRequest(
            type: "Email",
            recipient: "test@example.com",
            subject: "Test Notification",
            content: "This is a test notification"
        );

        // Act
        var result = await notificationClient.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.NotificationId);
        Assert.Equal("Sent", result.Status);
        
        // Verify notification status
        var status = await notificationClient.GetStatusAsync(result.NotificationId);
        Assert.Equal(result.NotificationId, status.NotificationId);
        Assert.True(status.IsDelivered);
    }

    [Fact]
    public async Task WorkflowClient_ShouldHandleQuoteWorkflow()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
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
        Assert.NotEmpty(workflowResult.WorkflowId);
        
        // Check workflow status
        var status = await workflowClient.GetWorkflowStatusAsync(workflowResult.WorkflowId);
        Assert.Equal(workflowResult.WorkflowId, status.WorkflowId);
    }

    [Fact]
    public async Task EndToEnd_OrderPlacementFlow_ShouldWork()
    {
        // Arrange
        var orderRequest = new
        {
            UserId = Guid.NewGuid(),
            AssetCode = "BTC",
            Quantity = 0.5m
        };

        // Act - Place order via API
        var response = await _client.PostAsJsonAsync("/api/order", orderRequest);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(orderResponse.TryGetProperty("orderId", out var orderIdProperty));
        Assert.True(orderResponse.TryGetProperty("status", out var statusProperty));
        Assert.Equal("Placed", statusProperty.GetString());
    }
}