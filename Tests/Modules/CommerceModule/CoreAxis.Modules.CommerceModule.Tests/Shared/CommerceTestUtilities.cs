using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.ValueObjects;
using CoreAxis.Modules.CommerceModule.Infrastructure.ExternalServices;
using Bogus;
using System.Diagnostics;

namespace CoreAxis.Modules.CommerceModule.Tests.Shared;

/// <summary>
/// Utility class for creating test data and common test operations
/// </summary>
public static class CommerceTestUtilities
{
    private static readonly Random _random = new Random();

    #region Data Generators

    /// <summary>
    /// Creates a Faker for generating Customer test data
    /// </summary>
    public static Faker<Customer> GetCustomerFaker()
    {
        return new Faker<Customer>()
            .RuleFor(c => c.Id, f => Guid.NewGuid())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.FirstName, f => f.Name.FirstName())
            .RuleFor(c => c.LastName, f => f.Name.LastName())
            .RuleFor(c => c.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(c => c.DateOfBirth, f => f.Date.Past(50, DateTime.Now.AddYears(-18)))
            .RuleFor(c => c.Segment, f => f.PickRandom("Standard", "Premium", "VIP", "Enterprise"))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(2))
            .RuleFor(c => c.UpdatedAt, f => f.Date.Recent(30))
            .RuleFor(c => c.IsActive, f => f.Random.Bool(0.9f));
    }

    /// <summary>
    /// Creates a Faker for generating InventoryItem test data
    /// </summary>
    public static Faker<InventoryItem> GetInventoryItemFaker()
    {
        return new Faker<InventoryItem>()
            .RuleFor(i => i.Id, f => Guid.NewGuid())
            .RuleFor(i => i.ProductId, f => Guid.NewGuid())
            .RuleFor(i => i.Sku, f => f.Commerce.Ean13())
            .RuleFor(i => i.QuantityOnHand, f => f.Random.Int(0, 1000))
            .RuleFor(i => i.QuantityReserved, f => f.Random.Int(0, 50))
            .RuleFor(i => i.ReorderLevel, f => f.Random.Int(10, 100))
            .RuleFor(i => i.MaxStockLevel, f => f.Random.Int(500, 2000))
            .RuleFor(i => i.Location, f => f.PickRandom("Warehouse A", "Warehouse B", "Warehouse C", "Store Front"))
            .RuleFor(i => i.LastUpdated, f => f.Date.Recent(30))
            .FinishWith((f, i) => i.QuantityAvailable = i.QuantityOnHand - i.QuantityReserved);
    }

    /// <summary>
    /// Creates a Faker for generating Order test data
    /// </summary>
    public static Faker<Order> GetOrderFaker()
    {
        return new Faker<Order>()
            .RuleFor(o => o.Id, f => Guid.NewGuid())
            .RuleFor(o => o.OrderNumber, f => f.Commerce.Random.AlphaNumeric(10).ToUpper())
            .RuleFor(o => o.CustomerId, f => Guid.NewGuid())
            .RuleFor(o => o.Status, f => f.PickRandom<OrderStatus>())
            .RuleFor(o => o.SubTotal, f => f.Random.Decimal(10, 1000))
            .RuleFor(o => o.TaxAmount, f => f.Random.Decimal(0, 100))
            .RuleFor(o => o.ShippingAmount, f => f.Random.Decimal(0, 50))
            .RuleFor(o => o.DiscountAmount, f => f.Random.Decimal(0, 100))
            .RuleFor(o => o.Currency, f => f.PickRandom("USD", "EUR", "GBP", "CAD"))
            .RuleFor(o => o.CreatedAt, f => f.Date.Past(1))
            .RuleFor(o => o.UpdatedAt, f => f.Date.Recent(30))
            .RuleFor(o => o.Items, f => new List<OrderItem>())
            .FinishWith((f, o) => o.TotalAmount = o.SubTotal + o.TaxAmount + o.ShippingAmount - o.DiscountAmount);
    }

    /// <summary>
    /// Creates a Faker for generating OrderItem test data
    /// </summary>
    public static Faker<OrderItem> GetOrderItemFaker(Guid? orderId = null)
    {
        return new Faker<OrderItem>()
            .RuleFor(oi => oi.Id, f => Guid.NewGuid())
            .RuleFor(oi => oi.OrderId, f => orderId ?? Guid.NewGuid())
            .RuleFor(oi => oi.ProductId, f => Guid.NewGuid())
            .RuleFor(oi => oi.Sku, f => f.Commerce.Ean13())
            .RuleFor(oi => oi.ProductName, f => f.Commerce.ProductName())
            .RuleFor(oi => oi.Quantity, f => f.Random.Int(1, 10))
            .RuleFor(oi => oi.UnitPrice, f => f.Random.Decimal(5, 500))
            .FinishWith((f, oi) => oi.TotalPrice = oi.Quantity * oi.UnitPrice);
    }

    /// <summary>
    /// Creates a Faker for generating SubscriptionPlan test data
    /// </summary>
    public static Faker<SubscriptionPlan> GetSubscriptionPlanFaker()
    {
        return new Faker<SubscriptionPlan>()
            .RuleFor(sp => sp.Id, f => Guid.NewGuid())
            .RuleFor(sp => sp.Name, f => f.Commerce.ProductName() + " Plan")
            .RuleFor(sp => sp.Description, f => f.Lorem.Sentence(10))
            .RuleFor(sp => sp.Price, f => f.Random.Decimal(9.99m, 999.99m))
            .RuleFor(sp => sp.Currency, f => f.PickRandom("USD", "EUR", "GBP"))
            .RuleFor(sp => sp.BillingCycle, f => f.PickRandom<BillingCycle>())
            .RuleFor(sp => sp.TrialPeriodDays, f => f.Random.Int(0, 30))
            .RuleFor(sp => sp.IsActive, f => f.Random.Bool(0.8f))
            .RuleFor(sp => sp.CreatedAt, f => f.Date.Past(2))
            .RuleFor(sp => sp.UpdatedAt, f => f.Date.Recent(30));
    }

    /// <summary>
    /// Creates a Faker for generating Subscription test data
    /// </summary>
    public static Faker<Subscription> GetSubscriptionFaker()
    {
        return new Faker<Subscription>()
            .RuleFor(s => s.Id, f => Guid.NewGuid())
            .RuleFor(s => s.CustomerId, f => Guid.NewGuid())
            .RuleFor(s => s.PlanId, f => Guid.NewGuid())
            .RuleFor(s => s.Status, f => f.PickRandom<SubscriptionStatus>())
            .RuleFor(s => s.StartDate, f => f.Date.Past(1))
            .RuleFor(s => s.EndDate, f => f.Date.Future(1))
            .RuleFor(s => s.TrialEndsAt, f => f.Date.Future(0.1f))
            .RuleFor(s => s.NextBillingDate, f => f.Date.Future(0.2f))
            .RuleFor(s => s.CreatedAt, f => f.Date.Past(1))
            .RuleFor(s => s.UpdatedAt, f => f.Date.Recent(30));
    }

    /// <summary>
    /// Creates a Faker for generating DiscountRule test data
    /// </summary>
    public static Faker<DiscountRule> GetDiscountRuleFaker()
    {
        return new Faker<DiscountRule>()
            .RuleFor(dr => dr.Id, f => Guid.NewGuid())
            .RuleFor(dr => dr.Name, f => f.Commerce.ProductAdjective() + " Discount")
            .RuleFor(dr => dr.Description, f => f.Lorem.Sentence(8))
            .RuleFor(dr => dr.DiscountType, f => f.PickRandom<DiscountType>())
            .RuleFor(dr => dr.Value, f => f.Random.Decimal(5, 50))
            .RuleFor(dr => dr.MinimumOrderAmount, f => f.Random.Decimal(0, 100))
            .RuleFor(dr => dr.MaxDiscountAmount, f => f.Random.Decimal(10, 200))
            .RuleFor(dr => dr.ValidFrom, f => f.Date.Past(0.5f))
            .RuleFor(dr => dr.ValidTo, f => f.Date.Future(0.5f))
            .RuleFor(dr => dr.MaxUsageCount, f => f.Random.Int(10, 1000))
            .RuleFor(dr => dr.UsageCount, f => f.Random.Int(0, 50))
            .RuleFor(dr => dr.IsActive, f => f.Random.Bool(0.8f))
            .RuleFor(dr => dr.ApplicableToAllProducts, f => f.Random.Bool(0.7f))
            .RuleFor(dr => dr.CustomerSegments, f => f.Make(3, () => f.PickRandom("Standard", "Premium", "VIP")).ToList())
            .RuleFor(dr => dr.ExcludedProductIds, f => f.Make(2, () => Guid.NewGuid()).ToList());
    }

    /// <summary>
    /// Creates a Faker for generating Coupon test data
    /// </summary>
    public static Faker<Coupon> GetCouponFaker()
    {
        return new Faker<Coupon>()
            .RuleFor(c => c.Id, f => Guid.NewGuid())
            .RuleFor(c => c.Code, f => f.Random.AlphaNumeric(8).ToUpper())
            .RuleFor(c => c.DiscountRuleId, f => Guid.NewGuid())
            .RuleFor(c => c.IsActive, f => f.Random.Bool(0.8f))
            .RuleFor(c => c.ValidFrom, f => f.Date.Past(0.5f))
            .RuleFor(c => c.ValidTo, f => f.Date.Future(0.5f))
            .RuleFor(c => c.MaxUsageCount, f => f.Random.Int(10, 500))
            .RuleFor(c => c.UsageCount, f => f.Random.Int(0, 20))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(1))
            .RuleFor(c => c.UpdatedAt, f => f.Date.Recent(30));
    }

    /// <summary>
    /// Creates a Faker for generating Payment test data
    /// </summary>
    public static Faker<Payment> GetPaymentFaker()
    {
        return new Faker<Payment>()
            .RuleFor(p => p.Id, f => Guid.NewGuid())
            .RuleFor(p => p.OrderId, f => Guid.NewGuid())
            .RuleFor(p => p.Amount, f => f.Random.Decimal(10, 1000))
            .RuleFor(p => p.Currency, f => f.PickRandom("USD", "EUR", "GBP"))
            .RuleFor(p => p.PaymentProvider, f => f.PickRandom("Stripe", "PayPal", "Square", "Wallet"))
            .RuleFor(p => p.PaymentMethodId, f => "pm_" + f.Random.AlphaNumeric(24))
            .RuleFor(p => p.ExternalTransactionId, f => f.Random.AlphaNumeric(32))
            .RuleFor(p => p.Status, f => f.PickRandom<PaymentStatus>())
            .RuleFor(p => p.CreatedAt, f => f.Date.Past(1))
            .RuleFor(p => p.ProcessedAt, f => f.Date.Recent(30));
    }

    /// <summary>
    /// Creates a Faker for generating RefundRequest test data
    /// </summary>
    public static Faker<RefundRequest> GetRefundRequestFaker()
    {
        return new Faker<RefundRequest>()
            .RuleFor(rr => rr.Id, f => Guid.NewGuid())
            .RuleFor(rr => rr.OrderId, f => Guid.NewGuid())
            .RuleFor(rr => rr.PaymentId, f => Guid.NewGuid())
            .RuleFor(rr => rr.Amount, f => f.Random.Decimal(10, 500))
            .RuleFor(rr => rr.Reason, f => f.PickRandom(
                "Customer requested refund",
                "Product defective",
                "Wrong item shipped",
                "Order cancelled",
                "Duplicate payment"))
            .RuleFor(rr => rr.RefundMethod, f => f.PickRandom<RefundMethod>())
            .RuleFor(rr => rr.Status, f => f.PickRandom<RefundStatus>())
            .RuleFor(rr => rr.RequestedBy, f => f.Internet.Email())
            .RuleFor(rr => rr.RestoreInventory, f => f.Random.Bool(0.7f))
            .RuleFor(rr => rr.CreatedAt, f => f.Date.Past(0.5f))
            .RuleFor(rr => rr.ProcessedAt, f => f.Date.Recent(30));
    }

    #endregion

    #region Test Data Builders

    /// <summary>
    /// Creates a complete order with items for testing
    /// </summary>
    public static Order CreateCompleteOrder(int itemCount = 3, string currency = "USD")
    {
        var order = GetOrderFaker().Generate();
        order.Currency = currency;
        order.Items = GetOrderItemFaker(order.Id).Generate(itemCount);
        
        // Recalculate totals based on items
        order.SubTotal = order.Items.Sum(i => i.TotalPrice);
        order.TotalAmount = order.SubTotal + order.TaxAmount + order.ShippingAmount - order.DiscountAmount;
        
        return order;
    }

    /// <summary>
    /// Creates a subscription with plan for testing
    /// </summary>
    public static (Subscription subscription, SubscriptionPlan plan) CreateSubscriptionWithPlan()
    {
        var plan = GetSubscriptionPlanFaker().Generate();
        var subscription = GetSubscriptionFaker().Generate();
        subscription.PlanId = plan.Id;
        subscription.Plan = plan;
        
        return (subscription, plan);
    }

    /// <summary>
    /// Creates a discount rule with coupon for testing
    /// </summary>
    public static (DiscountRule discountRule, Coupon coupon) CreateDiscountRuleWithCoupon()
    {
        var discountRule = GetDiscountRuleFaker().Generate();
        var coupon = GetCouponFaker().Generate();
        coupon.DiscountRuleId = discountRule.Id;
        coupon.DiscountRule = discountRule;
        
        return (discountRule, coupon);
    }

    /// <summary>
    /// Creates inventory items for multiple locations
    /// </summary>
    public static List<InventoryItem> CreateMultiLocationInventory(Guid productId, string sku)
    {
        var locations = new[] { "Warehouse A", "Warehouse B", "Warehouse C", "Store Front" };
        var items = new List<InventoryItem>();
        
        foreach (var location in locations)
        {
            var item = GetInventoryItemFaker().Generate();
            item.ProductId = productId;
            item.Sku = sku;
            item.Location = location;
            items.Add(item);
        }
        
        return items;
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Generates a unique SKU for testing
    /// </summary>
    public static string GenerateTestSku(string prefix = "TEST")
    {
        return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(1000, 9999)}";
    }

    /// <summary>
    /// Generates a unique order number for testing
    /// </summary>
    public static string GenerateTestOrderNumber(string prefix = "ORDER")
    {
        return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{_random.Next(100000, 999999)}";
    }

    /// <summary>
    /// Generates a unique coupon code for testing
    /// </summary>
    public static string GenerateTestCouponCode(string prefix = "TEST")
    {
        return $"{prefix}{_random.Next(1000, 9999)}";
    }

    /// <summary>
    /// Creates a mock payment response for testing
    /// </summary>
    public static PaymentResponse CreateMockPaymentResponse(
        bool success = true, 
        decimal amount = 100.00m, 
        string currency = "USD")
    {
        return new PaymentResponse
        {
            Success = success,
            TransactionId = success ? $"txn_{Guid.NewGuid().ToString()[..8]}" : null,
            Amount = amount,
            Currency = currency,
            Status = success ? "succeeded" : "failed",
            ErrorMessage = success ? null : "Payment processing failed",
            ProcessedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a mock refund response for testing
    /// </summary>
    public static RefundResponse CreateMockRefundResponse(
        bool success = true, 
        decimal amount = 100.00m, 
        string currency = "USD")
    {
        return new RefundResponse
        {
            Success = success,
            RefundId = success ? $"re_{Guid.NewGuid().ToString()[..8]}" : null,
            Amount = amount,
            Currency = currency,
            Status = success ? "succeeded" : "failed",
            ErrorMessage = success ? null : "Refund processing failed",
            ProcessedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a mock wallet transaction result for testing
    /// </summary>
    public static WalletTransactionResult CreateMockWalletTransactionResult(
        bool success = true, 
        decimal amount = 100.00m, 
        decimal balance = 500.00m)
    {
        return new WalletTransactionResult
        {
            Success = success,
            TransactionId = success ? Guid.NewGuid() : null,
            Amount = amount,
            Balance = success ? balance : 0,
            ErrorMessage = success ? null : "Insufficient wallet balance"
        };
    }

    /// <summary>
    /// Creates gateway transactions for reconciliation testing
    /// </summary>
    public static List<GatewayTransaction> CreateGatewayTransactions(
        int count, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow;
        var transactions = new List<GatewayTransaction>();
        
        for (int i = 0; i < count; i++)
        {
            var processedAt = start.AddTicks((long)((end - start).Ticks * _random.NextDouble()));
            var amount = (decimal)(_random.NextDouble() * 500 + 10); // $10 - $510
            
            transactions.Add(new GatewayTransaction
            {
                TransactionId = $"txn_{Guid.NewGuid().ToString()[..12]}",
                Amount = Math.Round(amount, 2),
                Currency = "USD",
                Status = _random.NextDouble() > 0.05 ? "completed" : "failed", // 95% success rate
                ProcessedAt = processedAt,
                OrderReference = $"ORDER-{processedAt:yyyyMMdd}-{_random.Next(100000, 999999)}",
                PaymentMethodId = $"pm_{Guid.NewGuid().ToString()[..16]}",
                Fees = Math.Round(amount * 0.029m + 0.30m, 2) // Typical payment processing fees
            });
        }
        
        return transactions.OrderBy(t => t.ProcessedAt).ToList();
    }

    #endregion

    #region Performance Helpers

    /// <summary>
    /// Measures execution time of an action
    /// </summary>
    public static TimeSpan MeasureExecutionTime(Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Measures execution time of an async action
    /// </summary>
    public static async Task<TimeSpan> MeasureExecutionTimeAsync(Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Creates a large dataset for performance testing
    /// </summary>
    public static List<T> CreateLargeDataset<T>(Faker<T> faker, int count) where T : class
    {
        return faker.Generate(count);
    }

    /// <summary>
    /// Simulates concurrent operations for stress testing
    /// </summary>
    public static async Task<List<TResult>> SimulateConcurrentOperations<TResult>(
        Func<Task<TResult>> operation, 
        int concurrencyLevel, 
        int totalOperations)
    {
        var semaphore = new SemaphoreSlim(concurrencyLevel);
        var tasks = new List<Task<TResult>>();
        
        for (int i = 0; i < totalOperations; i++)
        {
            tasks.Add(ExecuteWithSemaphore(operation, semaphore));
        }
        
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    private static async Task<TResult> ExecuteWithSemaphore<TResult>(
        Func<Task<TResult>> operation, 
        SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            return await operation();
        }
        finally
        {
            semaphore.Release();
        }
    }

    #endregion

    #region Validation Helpers

    /// <summary>
    /// Validates that an order is properly structured
    /// </summary>
    public static bool IsValidOrder(Order order)
    {
        if (order == null) return false;
        if (order.Id == Guid.Empty) return false;
        if (string.IsNullOrWhiteSpace(order.OrderNumber)) return false;
        if (order.CustomerId == Guid.Empty) return false;
        if (order.TotalAmount < 0) return false;
        if (string.IsNullOrWhiteSpace(order.Currency)) return false;
        
        // Validate items
        if (order.Items?.Any() == true)
        {
            foreach (var item in order.Items)
            {
                if (item.OrderId != order.Id) return false;
                if (item.Quantity <= 0) return false;
                if (item.UnitPrice < 0) return false;
                if (Math.Abs(item.TotalPrice - (item.Quantity * item.UnitPrice)) > 0.01m) return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Validates that inventory calculations are correct
    /// </summary>
    public static bool IsValidInventoryState(InventoryItem inventory)
    {
        if (inventory == null) return false;
        if (inventory.QuantityOnHand < 0) return false;
        if (inventory.QuantityReserved < 0) return false;
        if (inventory.QuantityReserved > inventory.QuantityOnHand) return false;
        if (inventory.QuantityAvailable != (inventory.QuantityOnHand - inventory.QuantityReserved)) return false;
        
        return true;
    }

    /// <summary>
    /// Validates that payment amounts match order totals
    /// </summary>
    public static bool ValidatePaymentAmounts(Order order, List<Payment> payments)
    {
        if (order == null || payments == null) return false;
        
        var successfulPayments = payments.Where(p => p.Status == PaymentStatus.Completed).ToList();
        var totalPaid = successfulPayments.Sum(p => p.Amount);
        
        return Math.Abs(totalPaid - order.TotalAmount) < 0.01m;
    }

    #endregion

    #region Constants

    public static class TestConstants
    {
        public const string DefaultCurrency = "USD";
        public const string DefaultLocation = "Test Warehouse";
        public const int DefaultInventoryQuantity = 100;
        public const decimal DefaultProductPrice = 50.00m;
        public const int DefaultTestTimeout = 30000; // 30 seconds
        public const int DefaultConcurrencyLevel = 10;
        public const int DefaultStressTestOperations = 1000;
        
        public static readonly string[] SupportedCurrencies = { "USD", "EUR", "GBP", "CAD" };
        public static readonly string[] TestLocations = { "Warehouse A", "Warehouse B", "Warehouse C", "Store Front" };
        public static readonly string[] CustomerSegments = { "Standard", "Premium", "VIP", "Enterprise" };
        public static readonly string[] PaymentProviders = { "Stripe", "PayPal", "Square", "Wallet" };
    }

    #endregion
}