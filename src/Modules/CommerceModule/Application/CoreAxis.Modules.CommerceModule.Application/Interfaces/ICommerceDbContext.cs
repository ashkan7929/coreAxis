using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

/// <summary>
/// Database context interface for commerce module.
/// </summary>
public interface ICommerceDbContext
{
    // Core entities
    DbSet<Order> Orders { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentIntent> PaymentIntents { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<InventoryReservation> InventoryReservations { get; }
    DbSet<InventoryLedger> InventoryLedgers { get; }
    
    // Subscription entities
    DbSet<Subscription> Subscriptions { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<SubscriptionInvoice> SubscriptionInvoices { get; }
    
    // Discount and coupon entities
    DbSet<DiscountRule> DiscountRules { get; }
    DbSet<CouponRedemption> CouponRedemptions { get; }
    
    // Refund entities
    DbSet<RefundRequest> RefundRequests { get; }
    DbSet<RefundLineItem> RefundLineItems { get; }
    
    // Split payment entities
    DbSet<SplitPaymentRule> SplitPaymentRules { get; }
    DbSet<SplitPaymentAllocation> SplitPaymentAllocations { get; }
    
    // Reconciliation entities
    DbSet<ReconciliationSession> ReconciliationSessions { get; }
    DbSet<ReconciliationEntry> ReconciliationEntries { get; }
    
    // Database operations
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}