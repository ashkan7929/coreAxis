using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.CommerceModule.Domain.Events;

/// <summary>
/// Event raised when a new subscription is created.
/// </summary>
public class SubscriptionCreatedEvent : DomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public Guid SubscriptionPlanId { get; }
    public string PlanName { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public DateTime StartDate { get; }
    public DateTime NextBillingDate { get; }
    public DateTime? TrialEndDate { get; }
    public DateTime CreatedAt { get; }

    public SubscriptionCreatedEvent(Guid subscriptionid, Guid customerid, Guid subscriptionplanid, string planname, decimal amount, string currency, DateTime startdate, DateTime nextbillingdate, DateTime? trialenddate, DateTime createdat)
    {
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        SubscriptionPlanId = subscriptionplanid;
        PlanName = planname;
        Amount = amount;
        Currency = currency;
        StartDate = startdate;
        NextBillingDate = nextbillingdate;
        TrialEndDate = trialenddate;
        CreatedAt = createdat;
    }
}

/// <summary>
/// Event raised when a subscription is canceled.
/// </summary>
public class SubscriptionCanceledEvent : DomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public Guid SubscriptionPlanId { get; }
    public string CancellationReason { get; }
    public bool CancelAtPeriodEnd { get; }
    public DateTime CanceledDate { get; }

    public SubscriptionCanceledEvent(Guid subscriptionid, Guid customerid, Guid subscriptionplanid, string cancellationreason, bool cancelatperiodend, DateTime canceleddate)
    {
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        SubscriptionPlanId = subscriptionplanid;
        CancellationReason = cancellationreason;
        CancelAtPeriodEnd = cancelatperiodend;
        CanceledDate = canceleddate;
    }
}

/// <summary>
/// Event raised when a subscription is reactivated.
/// </summary>
public class SubscriptionReactivatedEvent : DomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public Guid SubscriptionPlanId { get; }
    public DateTime ReactivatedDate { get; }

    public SubscriptionReactivatedEvent(Guid subscriptionid, Guid customerid, Guid subscriptionplanid, DateTime reactivateddate)
    {
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        SubscriptionPlanId = subscriptionplanid;
        ReactivatedDate = reactivateddate;
    }
}

/// <summary>
/// Event raised when a subscription enters grace period due to payment failure.
/// </summary>
public class SubscriptionEnteredGracePeriodEvent : DomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public int FailedPaymentAttempts { get; }
    public DateTime GracePeriodStartDate { get; }
    public DateTime GracePeriodEndDate { get; }

    public SubscriptionEnteredGracePeriodEvent(Guid subscriptionid, Guid customerid, int failedpaymentattempts, DateTime graceperiodstartdate, DateTime graceperiodenddate)
    {
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        FailedPaymentAttempts = failedpaymentattempts;
        GracePeriodStartDate = graceperiodstartdate;
        GracePeriodEndDate = graceperiodenddate;
    }
}

/// <summary>
/// Event raised when a subscription trial period ends.
/// </summary>
public class SubscriptionTrialEndedEvent : DomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public Guid SubscriptionPlanId { get; }
    public DateTime TrialEndDate { get; }
    public DateTime NextBillingDate { get; }

    public SubscriptionTrialEndedEvent(Guid subscriptionid, Guid customerid, Guid subscriptionplanid, DateTime trialenddate, DateTime nextbillingdate)
    {
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        SubscriptionPlanId = subscriptionplanid;
        TrialEndDate = trialenddate;
        NextBillingDate = nextbillingdate;
    }
}

/// <summary>
/// Event raised when a subscription plan is changed.
/// </summary>
public class SubscriptionPlanChangedEvent : DomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public Guid OldPlanId { get; }
    public Guid NewPlanId { get; }
    public decimal OldAmount { get; }
    public decimal NewAmount { get; }
    public DateTime EffectiveDate { get; }

    public SubscriptionPlanChangedEvent(Guid subscriptionid, Guid customerid, Guid oldplanid, Guid newplanid, decimal oldamount, decimal newamount, DateTime effectivedate)
    {
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        OldPlanId = oldplanid;
        NewPlanId = newplanid;
        OldAmount = oldamount;
        NewAmount = newamount;
        EffectiveDate = effectivedate;
    }
}

/// <summary>
/// Event raised when a subscription invoice is created.
/// </summary>
public class SubscriptionInvoiceCreatedEvent : DomainEvent
{
    public Guid InvoiceId { get; }
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public string InvoiceNumber { get; }
    public decimal Total { get; }
    public string Currency { get; }
    public DateTime PeriodStart { get; }
    public DateTime PeriodEnd { get; }
    public DateTime DueDate { get; }
    public DateTime CreatedAt { get; }

    public SubscriptionInvoiceCreatedEvent(Guid invoiceid, Guid subscriptionid, Guid customerid, string invoicenumber, decimal total, string currency, DateTime periodstart, DateTime periodend, DateTime duedate, DateTime createdat)
    {
        InvoiceId = invoiceid;
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        InvoiceNumber = invoicenumber;
        Total = total;
        Currency = currency;
        PeriodStart = periodstart;
        PeriodEnd = periodend;
        DueDate = duedate;
        CreatedAt = createdat;
    }
}

/// <summary>
/// Event raised when a subscription invoice is paid.
/// </summary>
public class SubscriptionInvoicePaidEvent : DomainEvent
{
    public Guid InvoiceId { get; }
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public string InvoiceNumber { get; }
    public decimal AmountPaid { get; }
    public string Currency { get; }
    public string? ExternalPaymentId { get; }
    public DateTime PaidDate { get; }

    public SubscriptionInvoicePaidEvent(Guid invoiceid, Guid subscriptionid, Guid customerid, string invoicenumber, decimal amountpaid, string currency, string? externalpaymentid, DateTime paiddate)
    {
        InvoiceId = invoiceid;
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        InvoiceNumber = invoicenumber;
        AmountPaid = amountpaid;
        Currency = currency;
        ExternalPaymentId = externalpaymentid;
        PaidDate = paiddate;
    }
}

/// <summary>
/// Event raised when a subscription invoice payment fails.
/// </summary>
public class SubscriptionInvoicePaymentFailedEvent : DomainEvent
{
    public Guid InvoiceId { get; }
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public string InvoiceNumber { get; }
    public decimal AmountDue { get; }
    public string Currency { get; }
    public string ErrorMessage { get; }
    public int PaymentAttempts { get; }
    public DateTime FailedDate { get; }

    public SubscriptionInvoicePaymentFailedEvent(Guid invoiceid, Guid subscriptionid, Guid customerid, string invoicenumber, decimal amountdue, string currency, string errormessage, int paymentattempts, DateTime faileddate)
    {
        InvoiceId = invoiceid;
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        InvoiceNumber = invoicenumber;
        AmountDue = amountdue;
        Currency = currency;
        ErrorMessage = errormessage;
        PaymentAttempts = paymentattempts;
        FailedDate = faileddate;
    }
}

/// <summary>
/// Event raised when a subscription invoice is refunded.
/// </summary>
public class SubscriptionInvoiceRefundedEvent : DomainEvent
{
    public Guid InvoiceId { get; }
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public string InvoiceNumber { get; }
    public decimal RefundAmount { get; }
    public string Currency { get; }
    public string RefundReason { get; }
    public DateTime RefundedDate { get; }

    public SubscriptionInvoiceRefundedEvent(Guid invoiceid, Guid subscriptionid, Guid customerid, string invoicenumber, decimal refundamount, string currency, string refundreason, DateTime refundeddate)
    {
        InvoiceId = invoiceid;
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        InvoiceNumber = invoicenumber;
        RefundAmount = refundamount;
        Currency = currency;
        RefundReason = refundreason;
        RefundedDate = refundeddate;
    }
}

/// <summary>
/// Event raised when a subscription billing cycle advances.
/// </summary>
public class SubscriptionBillingCycleAdvancedEvent : DomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public int BillingCyclesCompleted { get; }
    public DateTime LastBillingDate { get; }
    public DateTime NextBillingDate { get; }
    public DateTime AdvancedDate { get; }

    public SubscriptionBillingCycleAdvancedEvent(Guid subscriptionid, Guid customerid, int billingcyclescompleted, DateTime lastbillingdate, DateTime nextbillingdate, DateTime advanceddate)
    {
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        BillingCyclesCompleted = billingcyclescompleted;
        LastBillingDate = lastbillingdate;
        NextBillingDate = nextbillingdate;
        AdvancedDate = advanceddate;
    }
}

/// <summary>
/// Event raised when a subscription reaches its maximum billing cycles.
/// </summary>
public class SubscriptionMaxCyclesReachedEvent : DomainEvent
{
    public Guid SubscriptionId { get; }
    public Guid CustomerId { get; }
    public Guid SubscriptionPlanId { get; }
    public int MaxBillingCycles { get; }
    public DateTime CompletedDate { get; }

    public SubscriptionMaxCyclesReachedEvent(Guid subscriptionid, Guid customerid, Guid subscriptionplanid, int maxbillingcycles, DateTime completeddate)
    {
        SubscriptionId = subscriptionid;
        CustomerId = customerid;
        SubscriptionPlanId = subscriptionplanid;
        MaxBillingCycles = maxbillingcycles;
        CompletedDate = completeddate;
    }
}

/// <summary>
/// Event raised when a subscription plan is created.
/// </summary>
public class SubscriptionPlanCreatedEvent : DomainEvent
{
    public Guid PlanId { get; }
    public string Name { get; }
    public decimal Price { get; }
    public string Currency { get; }
    public int BillingIntervalDays { get; }
    public int TrialPeriodDays { get; }
    public DateTime CreatedAt { get; }

    public SubscriptionPlanCreatedEvent(Guid planid, string name, decimal price, string currency, int billingintervaldays, int trialperioddays, DateTime createdat)
    {
        PlanId = planid;
        Name = name;
        Price = price;
        Currency = currency;
        BillingIntervalDays = billingintervaldays;
        TrialPeriodDays = trialperioddays;
        CreatedAt = createdat;
    }
}

/// <summary>
/// Event raised when a subscription plan is deactivated.
/// </summary>
public class SubscriptionPlanDeactivatedEvent : DomainEvent
{
    public Guid PlanId { get; }
    public string Name { get; }
    public int ActiveSubscriptions { get; }
    public DateTime DeactivatedAt { get; }

    public SubscriptionPlanDeactivatedEvent(Guid planid, string name, int activesubscriptions, DateTime deactivatedat)
    {
        PlanId = planid;
        Name = name;
        ActiveSubscriptions = activesubscriptions;
        DeactivatedAt = deactivatedat;
    }
}