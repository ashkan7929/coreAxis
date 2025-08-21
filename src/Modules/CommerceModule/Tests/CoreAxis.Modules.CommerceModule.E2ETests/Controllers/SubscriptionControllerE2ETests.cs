using CoreAxis.Modules.CommerceModule.E2ETests.Infrastructure;
using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using FluentAssertions;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.CommerceModule.E2ETests.Controllers;

public class SubscriptionControllerE2ETests : BaseE2ETest
{
    public SubscriptionControllerE2ETests(CommerceTestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateSubscription_WithValidData_ShouldReturnCreatedSubscription()
    {
        // Arrange
        var createDto = CreateValidSubscriptionDto();

        // Act
        var result = await PostAsync<SubscriptionDto>("/api/v1/subscriptions", createDto);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(createDto.CustomerId);
        result.PlanId.Should().Be(createDto.PlanId);
        result.Status.Should().Be(SubscriptionStatus.Active);
        result.BillingCycle.Should().Be(createDto.BillingCycle);
        result.Amount.Should().Be(createDto.Amount);

        // Verify in database
        var dbSubscription = await DbContext.Subscriptions.FirstOrDefaultAsync(x => x.Id == result.Id);
        dbSubscription.Should().NotBeNull();
        dbSubscription!.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetSubscription_WithValidId_ShouldReturnSubscription()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync();

        // Act
        var result = await GetAsync<SubscriptionDto>($"/api/v1/subscriptions/{subscription.Id}");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(subscription.Id);
        result.CustomerId.Should().Be(subscription.CustomerId);
        result.PlanId.Should().Be(subscription.PlanId);
        result.Status.Should().Be(subscription.Status);
    }

    [Fact]
    public async Task GetSubscription_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/api/v1/subscriptions/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSubscriptionsByCustomer_ShouldReturnCustomerSubscriptions()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var subscriptions = new List<Subscription>();
        
        for (int i = 0; i < 3; i++)
        {
            var subscription = await CreateTestSubscriptionAsync(customerId);
            subscriptions.Add(subscription);
        }

        // Act
        var result = await GetAsync<PagedResultDto<SubscriptionDto>>($"/api/v1/subscriptions/customer/{customerId}?page=1&pageSize=10");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.Items.Should().OnlyContain(x => x.CustomerId == customerId);
    }

    [Fact]
    public async Task UpdateSubscription_WithValidData_ShouldUpdateSubscription()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync();
        var updateDto = new UpdateSubscriptionDto
        {
            PlanId = Guid.NewGuid(),
            Amount = 199.99m,
            BillingCycle = BillingCycle.Yearly
        };

        // Act
        var response = await PutAsync($"/api/v1/subscriptions/{subscription.Id}", updateDto);
        response.EnsureSuccessStatusCode();

        // Assert
        var updatedSubscription = await GetAsync<SubscriptionDto>($"/api/v1/subscriptions/{subscription.Id}");
        updatedSubscription.PlanId.Should().Be(updateDto.PlanId);
        updatedSubscription.Amount.Should().Be(updateDto.Amount);
        updatedSubscription.BillingCycle.Should().Be(updateDto.BillingCycle);

        // Verify in database
        var dbSubscription = await DbContext.Subscriptions.FirstOrDefaultAsync(x => x.Id == subscription.Id);
        dbSubscription!.Amount.Should().Be(updateDto.Amount);
    }

    [Fact]
    public async Task CancelSubscription_WithValidId_ShouldCancelSubscription()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync();
        var cancelDto = new CancelSubscriptionDto
        {
            Reason = "Customer requested cancellation",
            CancellationDate = DateTime.UtcNow.AddDays(7) // Cancel at end of billing period
        };

        // Act
        var response = await PostAsync<object>($"/api/v1/subscriptions/{subscription.Id}/cancel", cancelDto);

        // Assert
        response.Should().NotBeNull();

        // Verify subscription is marked for cancellation
        var cancelledSubscription = await GetAsync<SubscriptionDto>($"/api/v1/subscriptions/{subscription.Id}");
        cancelledSubscription.Status.Should().Be(SubscriptionStatus.PendingCancellation);
    }

    [Fact]
    public async Task PauseSubscription_WithValidId_ShouldPauseSubscription()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync();
        var pauseDto = new PauseSubscriptionDto
        {
            Reason = "Customer requested pause",
            PauseUntil = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var response = await PostAsync<object>($"/api/v1/subscriptions/{subscription.Id}/pause", pauseDto);

        // Assert
        response.Should().NotBeNull();

        // Verify subscription is paused
        var pausedSubscription = await GetAsync<SubscriptionDto>($"/api/v1/subscriptions/{subscription.Id}");
        pausedSubscription.Status.Should().Be(SubscriptionStatus.Paused);
    }

    [Fact]
    public async Task ResumeSubscription_WithValidId_ShouldResumeSubscription()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync();
        
        // First pause the subscription
        await PostAsync<object>($"/api/v1/subscriptions/{subscription.Id}/pause", new PauseSubscriptionDto
        {
            Reason = "Test pause",
            PauseUntil = DateTime.UtcNow.AddDays(30)
        });

        // Act
        var response = await PostAsync<object>($"/api/v1/subscriptions/{subscription.Id}/resume", new {});

        // Assert
        response.Should().NotBeNull();

        // Verify subscription is resumed
        var resumedSubscription = await GetAsync<SubscriptionDto>($"/api/v1/subscriptions/{subscription.Id}");
        resumedSubscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task RenewSubscription_WithValidId_ShouldRenewSubscription()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync();
        var renewDto = new RenewSubscriptionDto
        {
            ExtendByDays = 30,
            ProrateBilling = true
        };

        // Act
        var response = await PostAsync<object>($"/api/v1/subscriptions/{subscription.Id}/renew", renewDto);

        // Assert
        response.Should().NotBeNull();

        // Verify subscription end date is extended
        var renewedSubscription = await GetAsync<SubscriptionDto>($"/api/v1/subscriptions/{subscription.Id}");
        renewedSubscription.EndDate.Should().BeAfter(subscription.EndDate);
    }

    [Fact]
    public async Task GetSubscriptionBillingHistory_ShouldReturnBillingRecords()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync();
        
        // Create some billing history
        await CreateTestBillingRecordAsync(subscription.Id);
        await CreateTestBillingRecordAsync(subscription.Id);

        // Act
        var result = await GetAsync<PagedResultDto<BillingRecordDto>>($"/api/v1/subscriptions/{subscription.Id}/billing-history?page=1&pageSize=10");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterOrEqualTo(2);
        result.Items.Should().OnlyContain(x => x.SubscriptionId == subscription.Id);
    }

    [Fact]
    public async Task GetUpcomingBilling_ShouldReturnNextBillingInfo()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync();

        // Act
        var result = await GetAsync<UpcomingBillingDto>($"/api/v1/subscriptions/{subscription.Id}/upcoming-billing");

        // Assert
        result.Should().NotBeNull();
        result.SubscriptionId.Should().Be(subscription.Id);
        result.NextBillingDate.Should().BeAfter(DateTime.UtcNow);
        result.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessSubscriptionBilling_WithValidId_ShouldCreateBillingRecord()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync();
        var billingDto = new ProcessBillingDto
        {
            BillingDate = DateTime.UtcNow,
            Amount = subscription.Amount,
            PaymentMethod = PaymentMethod.CreditCard
        };

        // Act
        var result = await PostAsync<BillingRecordDto>($"/api/v1/subscriptions/{subscription.Id}/process-billing", billingDto);

        // Assert
        result.Should().NotBeNull();
        result.SubscriptionId.Should().Be(subscription.Id);
        result.Amount.Should().Be(billingDto.Amount);
        result.Status.Should().Be(BillingStatus.Paid);

        // Verify in database
        var dbBillingRecord = await DbContext.BillingRecords.FirstOrDefaultAsync(x => x.Id == result.Id);
        dbBillingRecord.Should().NotBeNull();
        dbBillingRecord!.Status.Should().Be(BillingStatus.Paid);
    }

    [Theory]
    [InlineData(BillingCycle.Monthly, 30)]
    [InlineData(BillingCycle.Quarterly, 90)]
    [InlineData(BillingCycle.Yearly, 365)]
    public async Task CreateSubscription_WithDifferentBillingCycles_ShouldSetCorrectEndDate(BillingCycle billingCycle, int expectedDays)
    {
        // Arrange
        var createDto = CreateValidSubscriptionDto();
        createDto.BillingCycle = billingCycle;
        var startDate = DateTime.UtcNow;

        // Act
        var result = await PostAsync<SubscriptionDto>("/api/v1/subscriptions", createDto);

        // Assert
        result.Should().NotBeNull();
        result.BillingCycle.Should().Be(billingCycle);
        
        var expectedEndDate = startDate.AddDays(expectedDays);
        result.EndDate.Should().BeCloseTo(expectedEndDate, TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task GetSubscriptionMetrics_ShouldReturnAggregatedData()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        
        // Create multiple subscriptions
        await CreateTestSubscriptionAsync(customerId, SubscriptionStatus.Active);
        await CreateTestSubscriptionAsync(customerId, SubscriptionStatus.Paused);
        await CreateTestSubscriptionAsync(customerId, SubscriptionStatus.Cancelled);

        // Act
        var result = await GetAsync<SubscriptionMetricsDto>($"/api/v1/subscriptions/customer/{customerId}/metrics");

        // Assert
        result.Should().NotBeNull();
        result.TotalSubscriptions.Should().Be(3);
        result.ActiveSubscriptions.Should().Be(1);
        result.PausedSubscriptions.Should().Be(1);
        result.CancelledSubscriptions.Should().Be(1);
    }

    [Fact]
    public async Task CreateSubscription_WithInvalidBillingCycle_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = CreateValidSubscriptionDto();
        createDto.BillingCycle = (BillingCycle)999; // Invalid enum value

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/subscriptions", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSubscription_WithNegativeAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = CreateValidSubscriptionDto();
        createDto.Amount = -50m; // Invalid negative amount

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/subscriptions", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelSubscription_AlreadyCancelled_ShouldReturnBadRequest()
    {
        // Arrange
        var subscription = await CreateTestSubscriptionAsync();
        
        // First cancellation
        await PostAsync<object>($"/api/v1/subscriptions/{subscription.Id}/cancel", new CancelSubscriptionDto
        {
            Reason = "First cancellation"
        });

        // Act - Try to cancel again
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/subscriptions/{subscription.Id}/cancel", new CancelSubscriptionDto
        {
            Reason = "Second cancellation"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Subscription> CreateTestSubscriptionAsync(Guid? customerId = null, SubscriptionStatus? status = null)
    {
        var customer = customerId ?? Guid.NewGuid();
        var planId = Guid.NewGuid();
        var subscription = new Subscription(customer, planId, 99.99m, BillingCycle.Monthly);
        
        if (status.HasValue && status != SubscriptionStatus.Active)
        {
            switch (status.Value)
            {
                case SubscriptionStatus.Paused:
                    subscription.Pause("Test pause", DateTime.UtcNow.AddDays(30));
                    break;
                case SubscriptionStatus.Cancelled:
                    subscription.Cancel("Test cancellation");
                    break;
                case SubscriptionStatus.PendingCancellation:
                    subscription.ScheduleCancellation("Test scheduled cancellation", DateTime.UtcNow.AddDays(7));
                    break;
            }
        }
        
        await SeedSubscriptionAsync(subscription);
        return subscription;
    }

    private async Task<BillingRecord> CreateTestBillingRecordAsync(Guid subscriptionId)
    {
        var billingRecord = new BillingRecord(subscriptionId, 99.99m, DateTime.UtcNow);
        billingRecord.MarkAsPaid("test-payment-id");
        
        await SeedBillingRecordAsync(billingRecord);
        return billingRecord;
    }
}