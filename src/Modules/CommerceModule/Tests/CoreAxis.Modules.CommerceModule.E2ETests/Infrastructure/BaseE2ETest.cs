using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using CoreAxis.Modules.CommerceModule.Infrastructure.Data;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Bogus;
using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.E2ETests.Infrastructure;

public abstract class BaseE2ETest : IClassFixture<CommerceTestWebApplicationFactory>, IDisposable
{
    protected readonly HttpClient HttpClient;
    protected readonly CommerceTestWebApplicationFactory Factory;
    protected readonly IServiceScope Scope;
    protected readonly CommerceDbContext DbContext;
    protected readonly Faker Faker;

    protected BaseE2ETest(CommerceTestWebApplicationFactory factory)
    {
        Factory = factory;
        HttpClient = factory.CreateClient();
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        Faker = new Faker();
        
        // Set default authorization header
        HttpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
    }

    protected async Task<T> PostAsync<T>(string endpoint, object request)
    {
        var response = await HttpClient.PostAsJsonAsync(endpoint, request);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    protected async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await HttpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    protected async Task<HttpResponseMessage> PutAsync(string endpoint, object request)
    {
        return await HttpClient.PutAsJsonAsync(endpoint, request);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string endpoint)
    {
        return await HttpClient.DeleteAsync(endpoint);
    }

    protected CreateInventoryItemDto CreateValidInventoryItemDto()
    {
        return new CreateInventoryItemDto
        {
            Name = Faker.Commerce.ProductName(),
            Description = Faker.Commerce.ProductDescription(),
            Sku = Faker.Commerce.Ean13(),
            Price = Faker.Random.Decimal(10, 1000),
            Currency = "USD",
            Quantity = Faker.Random.Int(1, 100),
            LowStockThreshold = Faker.Random.Int(1, 10),
            Category = Faker.Commerce.Categories(1).First(),
            IsActive = true
        };
    }

    protected CreateOrderDto CreateValidOrderDto(List<Guid> inventoryItemIds)
    {
        return new CreateOrderDto
        {
            CustomerId = Guid.NewGuid(),
            Items = inventoryItemIds.Select(id => new OrderItemDto
            {
                InventoryItemId = id,
                Quantity = Faker.Random.Int(1, 5),
                UnitPrice = Faker.Random.Decimal(10, 100)
            }).ToList(),
            ShippingAddress = new AddressDto
            {
                Street = Faker.Address.StreetAddress(),
                City = Faker.Address.City(),
                State = Faker.Address.State(),
                PostalCode = Faker.Address.ZipCode(),
                Country = Faker.Address.Country()
            },
            BillingAddress = new AddressDto
            {
                Street = Faker.Address.StreetAddress(),
                City = Faker.Address.City(),
                State = Faker.Address.State(),
                PostalCode = Faker.Address.ZipCode(),
                Country = Faker.Address.Country()
            }
        };
    }

    protected ProcessPaymentDto CreateValidPaymentDto(Guid orderId, decimal amount)
    {
        return new ProcessPaymentDto
        {
            OrderId = orderId,
            Amount = amount,
            Currency = "USD",
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentDetails = new Dictionary<string, object>
            {
                { "CardNumber", "4111111111111111" },
                { "ExpiryMonth", "12" },
                { "ExpiryYear", "2025" },
                { "CVV", "123" },
                { "CardHolderName", Faker.Name.FullName() }
            }
        };
    }

    protected CreateSubscriptionDto CreateValidSubscriptionDto()
    {
        return new CreateSubscriptionDto
        {
            CustomerId = Guid.NewGuid(),
            PlanName = Faker.Commerce.ProductName(),
            BillingCycle = BillingCycle.Monthly,
            Amount = Faker.Random.Decimal(10, 100),
            Currency = "USD",
            StartDate = DateTime.UtcNow,
            PaymentMethod = PaymentMethod.CreditCard,
            PaymentDetails = new Dictionary<string, object>
            {
                { "CardNumber", "4111111111111111" },
                { "ExpiryMonth", "12" },
                { "ExpiryYear", "2025" },
                { "CVV", "123" }
            }
        };
    }

    protected async Task SeedInventoryItemAsync(InventoryItem item)
    {
        DbContext.InventoryItems.Add(item);
        await DbContext.SaveChangesAsync();
    }

    protected async Task SeedOrderAsync(Order order)
    {
        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();
    }

    protected async Task SeedPaymentAsync(Payment payment)
    {
        DbContext.Payments.Add(payment);
        await DbContext.SaveChangesAsync();
    }

    protected async Task SeedRefundAsync(Refund refund)
    {
        DbContext.Refunds.Add(refund);
        await DbContext.SaveChangesAsync();
    }

    protected async Task SeedSubscriptionAsync(Subscription subscription)
    {
        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();
    }

    protected async Task SeedBillingRecordAsync(BillingRecord billingRecord)
    {
        DbContext.BillingRecords.Add(billingRecord);
        await DbContext.SaveChangesAsync();
    }

    protected async Task SeedOrderAsync(Order order)
    {
        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();
    }

    protected async Task<InventoryItem> CreateTestInventoryItemAsync()
    {
        var item = new InventoryItem(
            Faker.Commerce.ProductName(),
            Faker.Commerce.ProductDescription(),
            Faker.Commerce.Ean13(),
            Faker.Random.Decimal(10, 1000),
            "USD",
            Faker.Random.Int(1, 100),
            Faker.Random.Int(1, 10),
            Faker.Commerce.Categories(1).First()
        );
        
        await SeedInventoryItemAsync(item);
        return item;
    }

    public void Dispose()
    {
        Scope?.Dispose();
        HttpClient?.Dispose();
    }
}