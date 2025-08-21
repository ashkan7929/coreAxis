using CoreAxis.Modules.CommerceModule.E2ETests.Infrastructure;
using CoreAxis.Modules.CommerceModule.Api.DTOs;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using FluentAssertions;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.CommerceModule.E2ETests.Controllers;

public class PaymentControllerE2ETests : BaseE2ETest
{
    public PaymentControllerE2ETests(CommerceTestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ProcessPayment_WithValidData_ShouldReturnProcessedPayment()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var paymentDto = ProcessPaymentDto(order.Id, order.TotalAmount);

        // Act
        var result = await PostAsync<PaymentDto>("/api/v1/payments/process", paymentDto);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(order.Id);
        result.Amount.Should().Be(order.TotalAmount);
        result.Status.Should().Be(PaymentStatus.Completed);
        result.PaymentMethod.Should().Be(paymentDto.PaymentMethod);

        // Verify in database
        var dbPayment = await DbContext.Payments.FirstOrDefaultAsync(x => x.Id == result.Id);
        dbPayment.Should().NotBeNull();
        dbPayment!.Status.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public async Task ProcessPayment_WithInvalidAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var paymentDto = ProcessPaymentDto(order.Id, -100m); // Invalid negative amount

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/payments/process", paymentDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProcessPayment_WithNonExistentOrder_ShouldReturnNotFound()
    {
        // Arrange
        var invalidOrderId = Guid.NewGuid();
        var paymentDto = ProcessPaymentDto(invalidOrderId, 100m);

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/payments/process", paymentDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPayment_WithValidId_ShouldReturnPayment()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var payment = await CreateTestPaymentAsync(order.Id, order.TotalAmount);

        // Act
        var result = await GetAsync<PaymentDto>($"/api/v1/payments/{payment.Id}");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(payment.Id);
        result.OrderId.Should().Be(payment.OrderId);
        result.Amount.Should().Be(payment.Amount);
    }

    [Fact]
    public async Task GetPayment_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/api/v1/payments/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaymentsByOrder_ShouldReturnOrderPayments()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        
        var payments = new List<Payment>();
        for (int i = 0; i < 3; i++)
        {
            var payment = await CreateTestPaymentAsync(order.Id, 50m);
            payments.Add(payment);
        }

        // Act
        var result = await GetAsync<List<PaymentDto>>($"/api/v1/payments/order/{order.Id}");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().OnlyContain(x => x.OrderId == order.Id);
    }

    [Fact]
    public async Task RefundPayment_WithValidData_ShouldCreateRefund()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var payment = await CreateTestPaymentAsync(order.Id, order.TotalAmount);
        
        var refundDto = new RefundPaymentDto
        {
            Amount = payment.Amount / 2, // Partial refund
            Reason = "Customer requested partial refund"
        };

        // Act
        var result = await PostAsync<RefundDto>($"/api/v1/payments/{payment.Id}/refund", refundDto);

        // Assert
        result.Should().NotBeNull();
        result.PaymentId.Should().Be(payment.Id);
        result.Amount.Should().Be(refundDto.Amount);
        result.Status.Should().Be(RefundStatus.Completed);
        result.Reason.Should().Be(refundDto.Reason);

        // Verify in database
        var dbRefund = await DbContext.Refunds.FirstOrDefaultAsync(x => x.Id == result.Id);
        dbRefund.Should().NotBeNull();
        dbRefund!.Status.Should().Be(RefundStatus.Completed);
    }

    [Fact]
    public async Task RefundPayment_WithAmountExceedingPayment_ShouldReturnBadRequest()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var payment = await CreateTestPaymentAsync(order.Id, 100m);
        
        var refundDto = new RefundPaymentDto
        {
            Amount = 150m, // More than payment amount
            Reason = "Invalid refund amount"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync($"/api/v1/payments/{payment.Id}/refund", refundDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRefund_WithValidId_ShouldReturnRefund()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var payment = await CreateTestPaymentAsync(order.Id, order.TotalAmount);
        var refund = await CreateTestRefundAsync(payment.Id, 50m);

        // Act
        var result = await GetAsync<RefundDto>($"/api/v1/payments/refunds/{refund.Id}");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(refund.Id);
        result.PaymentId.Should().Be(refund.PaymentId);
        result.Amount.Should().Be(refund.Amount);
    }

    [Fact]
    public async Task GetRefundsByPayment_ShouldReturnPaymentRefunds()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var payment = await CreateTestPaymentAsync(order.Id, order.TotalAmount);
        
        var refunds = new List<Refund>();
        for (int i = 0; i < 2; i++)
        {
            var refund = await CreateTestRefundAsync(payment.Id, 25m);
            refunds.Add(refund);
        }

        // Act
        var result = await GetAsync<List<RefundDto>>($"/api/v1/payments/{payment.Id}/refunds");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(x => x.PaymentId == payment.Id);
    }

    [Fact]
    public async Task ProcessSplitPayment_WithValidData_ShouldCreateMultiplePayments()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        
        var splitPaymentDto = new SplitPaymentDto
        {
            OrderId = order.Id,
            Payments = new List<PaymentSplitDto>
            {
                new PaymentSplitDto
                {
                    Amount = order.TotalAmount * 0.6m,
                    PaymentMethod = PaymentMethod.CreditCard,
                    PaymentDetails = new Dictionary<string, object>
                    {
                        { "CardNumber", "**** **** **** 1234" },
                        { "ExpiryDate", "12/25" }
                    }
                },
                new PaymentSplitDto
                {
                    Amount = order.TotalAmount * 0.4m,
                    PaymentMethod = PaymentMethod.BankTransfer,
                    PaymentDetails = new Dictionary<string, object>
                    {
                        { "AccountNumber", "123456789" },
                        { "RoutingNumber", "987654321" }
                    }
                }
            }
        };

        // Act
        var result = await PostAsync<List<PaymentDto>>("/api/v1/payments/split", splitPaymentDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Sum(x => x.Amount).Should().Be(order.TotalAmount);
        result.Should().OnlyContain(x => x.OrderId == order.Id);
        result.Should().OnlyContain(x => x.Status == PaymentStatus.Completed);
    }

    [Fact]
    public async Task ProcessSplitPayment_WithAmountMismatch_ShouldReturnBadRequest()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        
        var splitPaymentDto = new SplitPaymentDto
        {
            OrderId = order.Id,
            Payments = new List<PaymentSplitDto>
            {
                new PaymentSplitDto
                {
                    Amount = order.TotalAmount * 0.5m, // Only 50% of total
                    PaymentMethod = PaymentMethod.CreditCard
                }
            }
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/v1/payments/split", splitPaymentDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPaymentHistory_ShouldReturnPaymentEvents()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var payment = await CreateTestPaymentAsync(order.Id, order.TotalAmount);
        
        // Create a refund to generate more history
        await CreateTestRefundAsync(payment.Id, 25m);

        // Act
        var result = await GetAsync<List<PaymentHistoryDto>>($"/api/v1/payments/{payment.Id}/history");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(1);
        result.Should().Contain(x => x.EventType == "PaymentProcessed");
    }

    [Theory]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.DebitCard)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.DigitalWallet)]
    [InlineData(PaymentMethod.Cash)]
    public async Task ProcessPayment_WithDifferentPaymentMethods_ShouldSucceed(PaymentMethod paymentMethod)
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var paymentDto = ProcessPaymentDto(order.Id, order.TotalAmount, paymentMethod);

        // Act
        var result = await PostAsync<PaymentDto>("/api/v1/payments/process", paymentDto);

        // Assert
        result.Should().NotBeNull();
        result.PaymentMethod.Should().Be(paymentMethod);
        result.Status.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public async Task ProcessPayment_WithIdempotencyKey_ShouldPreventDuplicatePayments()
    {
        // Arrange
        var inventoryItem = await CreateTestInventoryItemAsync();
        var order = await CreateTestOrderAsync(inventoryItem.Id);
        var paymentDto = ProcessPaymentDto(order.Id, order.TotalAmount);
        var idempotencyKey = Guid.NewGuid().ToString();

        // Add idempotency key to headers
        HttpClient.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        // Act - Process same payment twice
        var result1 = await PostAsync<PaymentDto>("/api/v1/payments/process", paymentDto);
        var result2 = await PostAsync<PaymentDto>("/api/v1/payments/process", paymentDto);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Id.Should().Be(result2.Id); // Should return same payment

        // Verify only one payment exists in database
        var paymentsCount = await DbContext.Payments.CountAsync(x => x.OrderId == order.Id);
        paymentsCount.Should().Be(1);
    }

    private async Task<Payment> CreateTestPaymentAsync(Guid orderId, decimal amount)
    {
        var payment = new Payment(orderId, amount, PaymentMethod.CreditCard);
        payment.MarkAsCompleted("test-transaction-id");
        
        await SeedPaymentAsync(payment);
        return payment;
    }

    private async Task<Refund> CreateTestRefundAsync(Guid paymentId, decimal amount)
    {
        var refund = new Refund(paymentId, amount, "Test refund");
        refund.MarkAsCompleted("test-refund-id");
        
        await SeedRefundAsync(refund);
        return refund;
    }

    private async Task<Order> CreateTestOrderAsync(Guid inventoryItemId)
    {
        var order = new Order(Guid.NewGuid());
        var orderItem = new OrderItem(inventoryItemId, 2, 50m);
        order.AddItem(orderItem);
        
        await SeedOrderAsync(order);
        return order;
    }
}