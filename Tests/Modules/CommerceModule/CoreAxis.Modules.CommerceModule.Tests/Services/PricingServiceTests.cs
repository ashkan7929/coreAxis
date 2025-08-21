using CoreAxis.Modules.CommerceModule.Application.Services;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Events;
using CoreAxis.Shared.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace CoreAxis.Modules.CommerceModule.Tests.Services;

public class PricingServiceTests
{
    private readonly Mock<ICommerceDbContext> _mockContext;
    private readonly Mock<IDomainEventDispatcher> _mockEventDispatcher;
    private readonly Mock<ILogger<PricingService>> _mockLogger;
    private readonly PricingService _service;

    public PricingServiceTests()
    {
        _mockContext = new Mock<ICommerceDbContext>();
        _mockEventDispatcher = new Mock<IDomainEventDispatcher>();
        _mockLogger = new Mock<ILogger<PricingService>>();
        _service = new PricingService(_mockContext.Object, _mockEventDispatcher.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ApplyDiscountsAsync_WithValidOrder_ShouldCalculatePricing()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Pending,
            LineItems = new List<OrderLineItem>
            {
                new OrderLineItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    Quantity = 2,
                    UnitPrice = 100.00m,
                    TotalPrice = 200.00m
                }
            }
        };

        var discountRule = new DiscountRule
        {
            Id = Guid.NewGuid(),
            Name = "10% Off",
            Type = DiscountType.Percentage,
            Value = 10,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(1),
            MinimumOrderAmount = 0,
            MaximumDiscountAmount = null,
            UsageLimit = null,
            UsageCount = 0
        };

        var context = new PricingContext
        {
            Order = order,
            CustomerId = customerId,
            CouponCodes = new List<string>(),
            ApplyAutomaticDiscounts = true
        };

        var mockDiscountSet = new Mock<DbSet<DiscountRule>>();
        var discountRules = new List<DiscountRule> { discountRule }.AsQueryable();
        
        _mockContext.Setup(c => c.DiscountRules).Returns(mockDiscountSet.Object);
        
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.Provider).Returns(discountRules.Provider);
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.Expression).Returns(discountRules.Expression);
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.ElementType).Returns(discountRules.ElementType);
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.GetEnumerator()).Returns(discountRules.GetEnumerator());

        // Act
        var result = await _service.ApplyDiscountsAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.BasePricing.Should().NotBeNull();
        result.FinalPricing.Should().NotBeNull();
        result.BasePricing.SubtotalAmount.Should().Be(200.00m);
        result.FinalPricing.TotalDiscountAmount.Should().Be(20.00m); // 10% of 200
        result.FinalPricing.FinalAmount.Should().Be(180.00m);
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<OrderPricingCalculatedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyDiscountsAsync_WithValidCoupon_ShouldApplyCouponDiscount()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var couponCode = "SAVE20";
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Pending,
            LineItems = new List<OrderLineItem>
            {
                new OrderLineItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    TotalPrice = 100.00m
                }
            }
        };

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = couponCode,
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 20.00m,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(1),
            UsageLimit = 100,
            UsageCount = 0,
            MinimumOrderAmount = 50.00m
        };

        var context = new PricingContext
        {
            Order = order,
            CustomerId = customerId,
            CouponCodes = new List<string> { couponCode },
            ApplyAutomaticDiscounts = false
        };

        var mockCouponSet = new Mock<DbSet<Coupon>>();
        var coupons = new List<Coupon> { coupon }.AsQueryable();
        
        _mockContext.Setup(c => c.Coupons).Returns(mockCouponSet.Object);
        
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.Provider).Returns(coupons.Provider);
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.Expression).Returns(coupons.Expression);
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.ElementType).Returns(coupons.ElementType);
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.GetEnumerator()).Returns(coupons.GetEnumerator());

        // Act
        var result = await _service.ApplyDiscountsAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.FinalPricing.TotalDiscountAmount.Should().Be(20.00m);
        result.FinalPricing.FinalAmount.Should().Be(80.00m);
        result.AppliedDiscounts.Should().HaveCount(1);
        result.AppliedDiscounts.First().DiscountSource.Should().Be("Coupon");
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<CouponRedeemedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyDiscountsAsync_WithInvalidCoupon_ShouldFailValidation()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var couponCode = "INVALID";
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Pending,
            LineItems = new List<OrderLineItem>
            {
                new OrderLineItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    TotalPrice = 100.00m
                }
            }
        };

        var context = new PricingContext
        {
            Order = order,
            CustomerId = customerId,
            CouponCodes = new List<string> { couponCode },
            ApplyAutomaticDiscounts = false
        };

        var mockCouponSet = new Mock<DbSet<Coupon>>();
        var coupons = new List<Coupon>().AsQueryable(); // Empty list
        
        _mockContext.Setup(c => c.Coupons).Returns(mockCouponSet.Object);
        
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.Provider).Returns(coupons.Provider);
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.Expression).Returns(coupons.Expression);
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.ElementType).Returns(coupons.ElementType);
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.GetEnumerator()).Returns(coupons.GetEnumerator());

        // Act
        var result = await _service.ApplyDiscountsAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid coupon");
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<CouponValidationFailedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyDiscountsAsync_WithExpiredCoupon_ShouldFailValidation()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var couponCode = "EXPIRED";
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Pending,
            LineItems = new List<OrderLineItem>
            {
                new OrderLineItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    TotalPrice = 100.00m
                }
            }
        };

        var expiredCoupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = couponCode,
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 20.00m,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-10),
            ValidTo = DateTime.UtcNow.AddDays(-1), // Expired
            UsageLimit = 100,
            UsageCount = 0
        };

        var context = new PricingContext
        {
            Order = order,
            CustomerId = customerId,
            CouponCodes = new List<string> { couponCode },
            ApplyAutomaticDiscounts = false
        };

        var mockCouponSet = new Mock<DbSet<Coupon>>();
        var coupons = new List<Coupon> { expiredCoupon }.AsQueryable();
        
        _mockContext.Setup(c => c.Coupons).Returns(mockCouponSet.Object);
        
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.Provider).Returns(coupons.Provider);
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.Expression).Returns(coupons.Expression);
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.ElementType).Returns(coupons.ElementType);
        mockCouponSet.As<IQueryable<Coupon>>()
            .Setup(m => m.GetEnumerator()).Returns(coupons.GetEnumerator());

        // Act
        var result = await _service.ApplyDiscountsAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expired");
    }

    [Fact]
    public async Task ApplyDiscountsAsync_WithMaxDiscountLimit_ShouldCapDiscount()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Pending,
            LineItems = new List<OrderLineItem>
            {
                new OrderLineItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 1000.00m,
                    TotalPrice = 1000.00m
                }
            }
        };

        var discountRule = new DiscountRule
        {
            Id = Guid.NewGuid(),
            Name = "50% Off with Cap",
            Type = DiscountType.Percentage,
            Value = 50, // 50% would be 500, but cap is 100
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(1),
            MinimumOrderAmount = 0,
            MaximumDiscountAmount = 100.00m, // Cap at 100
            UsageLimit = null,
            UsageCount = 0
        };

        var context = new PricingContext
        {
            Order = order,
            CustomerId = customerId,
            CouponCodes = new List<string>(),
            ApplyAutomaticDiscounts = true
        };

        var mockDiscountSet = new Mock<DbSet<DiscountRule>>();
        var discountRules = new List<DiscountRule> { discountRule }.AsQueryable();
        
        _mockContext.Setup(c => c.DiscountRules).Returns(mockDiscountSet.Object);
        
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.Provider).Returns(discountRules.Provider);
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.Expression).Returns(discountRules.Expression);
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.ElementType).Returns(discountRules.ElementType);
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.GetEnumerator()).Returns(discountRules.GetEnumerator());

        // Act
        var result = await _service.ApplyDiscountsAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.FinalPricing.TotalDiscountAmount.Should().Be(100.00m); // Capped at 100
        result.FinalPricing.FinalAmount.Should().Be(900.00m);
        
        _mockEventDispatcher.Verify(
            e => e.DispatchAsync(
                It.IsAny<MaxDiscountLimitExceededEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(DiscountType.Percentage, 10, 100, 10)] // 10% of 100 = 10
    [InlineData(DiscountType.FixedAmount, 15, 100, 15)] // Fixed 15
    [InlineData(DiscountType.Percentage, 25, 200, 50)] // 25% of 200 = 50
    public async Task CalculateDiscountAmount_WithDifferentTypes_ShouldCalculateCorrectly(
        DiscountType discountType, decimal discountValue, decimal orderAmount, decimal expectedDiscount)
    {
        // Arrange
        var discountRule = new DiscountRule
        {
            Type = discountType,
            Value = discountValue
        };

        var orderSnapshot = new OrderSnapshot
        {
            SubtotalAmount = orderAmount
        };

        // Act
        var result = _service.CalculateDiscountAmount(discountRule, orderSnapshot);

        // Assert
        result.Should().Be(expectedDiscount);
    }

    [Fact]
    public async Task ApplyDiscountsAsync_WithMinimumOrderAmount_ShouldRespectMinimum()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrderNumber = "ORD-001",
            Status = OrderStatus.Pending,
            LineItems = new List<OrderLineItem>
            {
                new OrderLineItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 50.00m,
                    TotalPrice = 50.00m
                }
            }
        };

        var discountRule = new DiscountRule
        {
            Id = Guid.NewGuid(),
            Name = "10% Off Min 100",
            Type = DiscountType.Percentage,
            Value = 10,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(1),
            MinimumOrderAmount = 100.00m, // Order is only 50, so discount shouldn't apply
            MaximumDiscountAmount = null,
            UsageLimit = null,
            UsageCount = 0
        };

        var context = new PricingContext
        {
            Order = order,
            CustomerId = customerId,
            CouponCodes = new List<string>(),
            ApplyAutomaticDiscounts = true
        };

        var mockDiscountSet = new Mock<DbSet<DiscountRule>>();
        var discountRules = new List<DiscountRule> { discountRule }.AsQueryable();
        
        _mockContext.Setup(c => c.DiscountRules).Returns(mockDiscountSet.Object);
        
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.Provider).Returns(discountRules.Provider);
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.Expression).Returns(discountRules.Expression);
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.ElementType).Returns(discountRules.ElementType);
        mockDiscountSet.As<IQueryable<DiscountRule>>()
            .Setup(m => m.GetEnumerator()).Returns(discountRules.GetEnumerator());

        // Act
        var result = await _service.ApplyDiscountsAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.FinalPricing.TotalDiscountAmount.Should().Be(0); // No discount applied
        result.FinalPricing.FinalAmount.Should().Be(50.00m);
        result.AppliedDiscounts.Should().BeEmpty();
    }
}