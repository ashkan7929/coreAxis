using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Events;
using CoreAxis.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAxis.Modules.CommerceModule.Application.Services;

/// <summary>
/// Background service for managing subscription scheduling and invoice generation.
/// </summary>
public class SubscriptionSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionSchedulerService> _logger;
    private readonly SubscriptionSchedulerOptions _options;
    private readonly Timer _schedulerTimer;

    public SubscriptionSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionSchedulerService> logger,
        IOptions<SubscriptionSchedulerOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        
        // Initialize timer for periodic execution
        _schedulerTimer = new Timer(
            ExecuteScheduledTasks,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(_options.CheckIntervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSubscriptionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing subscriptions");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.CheckIntervalMinutes), stoppingToken);
        }
    }

    /// <summary>
    /// Timer callback for executing scheduled tasks.
    /// </summary>
    private async void ExecuteScheduledTasks(object? state)
    {
        try
        {
            await ProcessSubscriptionsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled task execution");
        }
    }

    /// <summary>
    /// Main method for processing subscriptions.
    /// </summary>
    private async Task ProcessSubscriptionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        var eventDispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        var now = DateTime.UtcNow;
        var processingId = Guid.NewGuid().ToString();

        _logger.LogInformation("Starting subscription processing batch {ProcessingId} at {Timestamp}", 
            processingId, now);

        try
        {
            // Process due renewals
            await ProcessDueRenewalsAsync(context, subscriptionService, eventDispatcher, now, processingId, cancellationToken);

            // Process trial expirations
            await ProcessTrialExpirationsAsync(context, subscriptionService, eventDispatcher, now, processingId, cancellationToken);

            // Process payment failures
            await ProcessPaymentFailuresAsync(context, subscriptionService, eventDispatcher, now, processingId, cancellationToken);

            // Process subscription cancellations
            await ProcessPendingCancellationsAsync(context, subscriptionService, eventDispatcher, now, processingId, cancellationToken);

            // Cleanup expired data
            await CleanupExpiredDataAsync(context, now, processingId, cancellationToken);

            _logger.LogInformation("Completed subscription processing batch {ProcessingId}", processingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process subscription batch {ProcessingId}", processingId);
            throw;
        }
    }

    /// <summary>
    /// Processes subscriptions that are due for renewal.
    /// </summary>
    private async Task ProcessDueRenewalsAsync(
        ICommerceDbContext context,
        ISubscriptionService subscriptionService,
        IDomainEventDispatcher eventDispatcher,
        DateTime now,
        string processingId,
        CancellationToken cancellationToken)
    {
        var dueSubscriptions = await context.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.Customer)
            .Where(s => 
                s.Status == SubscriptionStatus.Active &&
                s.NextBillingDate <= now.AddMinutes(_options.RenewalLookaheadMinutes) &&
                !s.IsProcessing)
            .Take(_options.MaxBatchSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} subscriptions due for renewal in batch {ProcessingId}", 
            dueSubscriptions.Count, processingId);

        foreach (var subscription in dueSubscriptions)
        {
            try
            {
                // Mark as processing to prevent duplicate processing
                subscription.IsProcessing = true;
                subscription.LastProcessedAt = now;
                await context.SaveChangesAsync(cancellationToken);

                await ProcessSubscriptionRenewalAsync(
                    subscription, subscriptionService, eventDispatcher, now, processingId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to process renewal for subscription {SubscriptionId} in batch {ProcessingId}", 
                    subscription.Id, processingId);

                // Reset processing flag on error
                subscription.IsProcessing = false;
                await context.SaveChangesAsync(cancellationToken);

                await eventDispatcher.DispatchAsync(
                    new SubscriptionRenewalFailedEvent(
                        subscription.Id,
                        subscription.CustomerId,
                        ex.Message,
                        now,
                        processingId),
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// Processes renewal for a single subscription.
    /// </summary>
    private async Task ProcessSubscriptionRenewalAsync(
        Subscription subscription,
        ISubscriptionService subscriptionService,
        IDomainEventDispatcher eventDispatcher,
        DateTime now,
        string processingId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing renewal for subscription {SubscriptionId} in batch {ProcessingId}", 
            subscription.Id, processingId);

        // Calculate next billing period
        var nextBillingDate = CalculateNextBillingDate(subscription.NextBillingDate, subscription.Plan.BillingInterval);
        var billingPeriodStart = subscription.NextBillingDate;
        var billingPeriodEnd = nextBillingDate;

        // Create invoice for the billing period
        var invoice = new SubscriptionInvoice
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            CustomerId = subscription.CustomerId,
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            Amount = subscription.Plan.Price,
            Currency = subscription.Plan.Currency,
            BillingPeriodStart = billingPeriodStart,
            BillingPeriodEnd = billingPeriodEnd,
            DueDate = billingPeriodStart.AddDays(_options.InvoiceDueDays),
            Status = InvoiceStatus.Pending,
            // CreatedOn is inherited from EntityBase and set automatically
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                ProcessingId = processingId,
                PlanId = subscription.PlanId,
                BillingInterval = subscription.Plan.BillingInterval.ToString()
            })
        };

        // Add invoice line items
        invoice.LineItems.Add(new SubscriptionInvoiceLineItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Description = $"{subscription.Plan.Name} - {billingPeriodStart:yyyy-MM-dd} to {billingPeriodEnd:yyyy-MM-dd}",
            Quantity = 1,
            UnitPrice = subscription.Plan.Price,
            Amount = subscription.Plan.Price,
            PeriodStart = billingPeriodStart,
            PeriodEnd = billingPeriodEnd
        });

        // Save invoice
        await subscriptionService.CreateInvoiceAsync(invoice, cancellationToken);

        // Update subscription
        subscription.NextBillingDate = nextBillingDate;
        subscription.LastBillingDate = billingPeriodStart;
        subscription.IsProcessing = false;
        subscription.LastProcessedAt = now;

        // Dispatch events
        await eventDispatcher.DispatchAsync(
            new SubscriptionInvoiceCreatedEvent(
                invoice.Id,
                subscription.Id,
                subscription.CustomerId,
                invoice.Amount,
                invoice.Currency,
                billingPeriodStart,
                billingPeriodEnd,
                invoice.DueDate,
                now,
                processingId),
            cancellationToken);

        await eventDispatcher.DispatchAsync(
            new SubscriptionRenewedEvent(
                subscription.Id,
                subscription.CustomerId,
                subscription.PlanId,
                billingPeriodStart,
                billingPeriodEnd,
                nextBillingDate,
                invoice.Id,
                now,
                processingId),
            cancellationToken);

        _logger.LogInformation(
            "Successfully processed renewal for subscription {SubscriptionId}, created invoice {InvoiceId}", 
            subscription.Id, invoice.Id);
    }

    /// <summary>
    /// Processes subscriptions with expired trials.
    /// </summary>
    private async Task ProcessTrialExpirationsAsync(
        ICommerceDbContext context,
        ISubscriptionService subscriptionService,
        IDomainEventDispatcher eventDispatcher,
        DateTime now,
        string processingId,
        CancellationToken cancellationToken)
    {
        var expiredTrials = await context.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.Customer)
            .Where(s => 
                s.Status == SubscriptionStatus.Trialing &&
                s.TrialEndDate.HasValue &&
                s.TrialEndDate.Value <= now &&
                !s.IsProcessing)
            .Take(_options.MaxBatchSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} expired trials in batch {ProcessingId}", 
            expiredTrials.Count, processingId);

        foreach (var subscription in expiredTrials)
        {
            try
            {
                subscription.IsProcessing = true;
                await context.SaveChangesAsync(cancellationToken);

                // Convert trial to active subscription
                subscription.Status = SubscriptionStatus.Active;
                subscription.StartDate = now;
                subscription.NextBillingDate = CalculateNextBillingDate(now, subscription.Plan.BillingInterval);
                subscription.IsProcessing = false;
                subscription.LastProcessedAt = now;

                await eventDispatcher.DispatchAsync(
                    new SubscriptionTrialExpiredEvent(
                        subscription.Id,
                        subscription.CustomerId,
                        subscription.TrialEndDate.Value,
                        subscription.NextBillingDate,
                        now,
                        processingId),
                    cancellationToken);

                _logger.LogInformation(
                    "Converted trial subscription {SubscriptionId} to active", subscription.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to process trial expiration for subscription {SubscriptionId}", 
                    subscription.Id);

                subscription.IsProcessing = false;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Processes subscriptions with payment failures.
    /// </summary>
    private async Task ProcessPaymentFailuresAsync(
        ICommerceDbContext context,
        ISubscriptionService subscriptionService,
        IDomainEventDispatcher eventDispatcher,
        DateTime now,
        string processingId,
        CancellationToken cancellationToken)
    {
        var failedPayments = await context.SubscriptionInvoices
            .Include(i => i.Subscription)
            .ThenInclude(s => s.Plan)
            .Where(i => 
                i.Status == InvoiceStatus.PaymentFailed &&
                i.NextRetryDate.HasValue &&
                i.NextRetryDate.Value <= now &&
                i.RetryAttempts < _options.MaxPaymentRetries)
            .Take(_options.MaxBatchSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} failed payments to retry in batch {ProcessingId}", 
            failedPayments.Count, processingId);

        foreach (var invoice in failedPayments)
        {
            try
            {
                await subscriptionService.RetryPaymentAsync(invoice.Id, processingId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to retry payment for invoice {InvoiceId}", invoice.Id);
            }
        }
    }

    /// <summary>
    /// Processes subscriptions pending cancellation.
    /// </summary>
    private async Task ProcessPendingCancellationsAsync(
        ICommerceDbContext context,
        ISubscriptionService subscriptionService,
        IDomainEventDispatcher eventDispatcher,
        DateTime now,
        string processingId,
        CancellationToken cancellationToken)
    {
        var pendingCancellations = await context.Subscriptions
            .Where(s => 
                s.Status == SubscriptionStatus.PendingCancellation &&
                s.CancelAt.HasValue &&
                s.CancelAt.Value <= now)
            .Take(_options.MaxBatchSize)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} pending cancellations in batch {ProcessingId}", 
            pendingCancellations.Count, processingId);

        foreach (var subscription in pendingCancellations)
        {
            try
            {
                await subscriptionService.CancelSubscriptionAsync(
                    subscription.Id, "Scheduled cancellation", processingId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to cancel subscription {SubscriptionId}", subscription.Id);
            }
        }
    }

    /// <summary>
    /// Cleans up expired data.
    /// </summary>
    private async Task CleanupExpiredDataAsync(
        ICommerceDbContext context,
        DateTime now,
        string processingId,
        CancellationToken cancellationToken)
    {
        var cutoffDate = now.AddDays(-_options.DataRetentionDays);

        // Clean up old processing logs
        var expiredLogs = await context.SubscriptionProcessingLogs
            .Where(l => l.CreatedOn < cutoffDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        if (expiredLogs.Any())
        {
            context.SubscriptionProcessingLogs.RemoveRange(expiredLogs);
            await context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Cleaned up {Count} expired processing logs in batch {ProcessingId}", 
                expiredLogs.Count, processingId);
        }
    }

    /// <summary>
    /// Calculates the next billing date based on interval.
    /// </summary>
    private DateTime CalculateNextBillingDate(DateTime currentDate, BillingInterval interval)
    {
        return interval switch
        {
            BillingInterval.Daily => currentDate.AddDays(1),
            BillingInterval.Weekly => currentDate.AddDays(7),
            BillingInterval.Monthly => currentDate.AddMonths(1),
            BillingInterval.Quarterly => currentDate.AddMonths(3),
            BillingInterval.Yearly => currentDate.AddYears(1),
            _ => currentDate.AddMonths(1)
        };
    }

    /// <summary>
    /// Generates a unique invoice number.
    /// </summary>
    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"INV-{timestamp}-{random}";
    }

    public override void Dispose()
    {
        _schedulerTimer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Configuration options for the subscription scheduler.
/// </summary>
public class SubscriptionSchedulerOptions
{
    public const string SectionName = "SubscriptionScheduler";

    /// <summary>
    /// Interval in minutes between scheduler runs.
    /// </summary>
    public int CheckIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// How many minutes ahead to look for renewals.
    /// </summary>
    public int RenewalLookaheadMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum number of subscriptions to process in one batch.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Number of days after invoice creation when it's due.
    /// </summary>
    public int InvoiceDueDays { get; set; } = 7;

    /// <summary>
    /// Maximum number of payment retry attempts.
    /// </summary>
    public int MaxPaymentRetries { get; set; } = 3;

    /// <summary>
    /// Number of days to retain processing logs.
    /// </summary>
    public int DataRetentionDays { get; set; } = 90;
}



/// <summary>
/// Processing log entity for tracking scheduler operations.
/// </summary>
public class SubscriptionProcessingLog
{
    public Guid Id { get; set; }
    public string ProcessingId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public Guid? SubscriptionId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? MetadataJson { get; set; }
}