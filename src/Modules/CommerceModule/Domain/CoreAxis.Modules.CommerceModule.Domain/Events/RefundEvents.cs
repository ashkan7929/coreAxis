using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.CommerceModule.Domain.Events;

/// <summary>
/// Event raised when a refund request is created.
/// </summary>
public class RefundRequestCreatedEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public Guid? PaymentId { get; }
    public string RefundNumber { get; }
    public RefundType RefundType { get; }
    public RefundReason Reason { get; }
    public string? ReasonDescription { get; }
    public decimal OriginalAmount { get; }
    public decimal RequestedAmount { get; }
    public string Currency { get; }
    public RefundStatus Status { get; }
    public Guid RequestedByUserId { get; }
    public DateTime RequestedAt { get; }
    public RefundMethod RefundMethod { get; }
    public string? RefundDestination { get; }
    public bool RequiresApproval { get; }
    public RefundPriority Priority { get; }
    public string? CorrelationId { get; }
    public string? IdempotencyKey { get; }

    public RefundRequestCreatedEvent(Guid refundrequestid, Guid orderid, Guid? paymentid, string refundnumber, RefundType refundtype, RefundReason reason, string? reasondescription, decimal originalamount, decimal requestedamount, string currency, RefundStatus status, Guid requestedbyuserid, DateTime requestedat, RefundMethod refundmethod, string? refunddestination, bool requiresapproval, RefundPriority priority, string? correlationid, string? idempotencykey)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        PaymentId = paymentid;
        RefundNumber = refundnumber;
        RefundType = refundtype;
        Reason = reason;
        ReasonDescription = reasondescription;
        OriginalAmount = originalamount;
        RequestedAmount = requestedamount;
        Currency = currency;
        Status = status;
        RequestedByUserId = requestedbyuserid;
        RequestedAt = requestedat;
        RefundMethod = refundmethod;
        RefundDestination = refunddestination;
        RequiresApproval = requiresapproval;
        Priority = priority;
        CorrelationId = correlationid;
        IdempotencyKey = idempotencykey;
    }
}

/// <summary>
/// Event raised when a refund request is approved.
/// </summary>
public class RefundRequestApprovedEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public string RefundNumber { get; }
    public decimal RequestedAmount { get; }
    public decimal ApprovedAmount { get; }
    public string Currency { get; }
    public Guid ApprovedByUserId { get; }
    public DateTime ApprovedAt { get; }
    public string? ApprovalNotes { get; }
    public string? CorrelationId { get; }

    public RefundRequestApprovedEvent(Guid refundrequestid, Guid orderid, string refundnumber, decimal requestedamount, decimal approvedamount, string currency, Guid approvedbyuserid, DateTime approvedat, string? approvalnotes, string? correlationid)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        RefundNumber = refundnumber;
        RequestedAmount = requestedamount;
        ApprovedAmount = approvedamount;
        Currency = currency;
        ApprovedByUserId = approvedbyuserid;
        ApprovedAt = approvedat;
        ApprovalNotes = approvalnotes;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Event raised when a refund request is rejected.
/// </summary>
public class RefundRequestRejectedEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public string RefundNumber { get; }
    public decimal RequestedAmount { get; }
    public string Currency { get; }
    public Guid RejectedByUserId { get; }
    public DateTime RejectedAt { get; }
    public string RejectionReason { get; }
    public string? CorrelationId { get; }

    public RefundRequestRejectedEvent(Guid refundrequestid, Guid orderid, string refundnumber, decimal requestedamount, string currency, Guid rejectedbyuserid, DateTime rejectedat, string rejectionreason, string? correlationid)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        RefundNumber = refundnumber;
        RequestedAmount = requestedamount;
        Currency = currency;
        RejectedByUserId = rejectedbyuserid;
        RejectedAt = rejectedat;
        RejectionReason = rejectionreason;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Event raised when a refund request processing starts.
/// </summary>
public class RefundRequestProcessingStartedEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public string RefundNumber { get; }
    public decimal ApprovedAmount { get; }
    public string Currency { get; }
    public RefundMethod RefundMethod { get; }
    public string? RefundDestination { get; }
    public string? PaymentProvider { get; }
    public DateTime ProcessedAt { get; }
    public string? CorrelationId { get; }

    public RefundRequestProcessingStartedEvent(Guid refundrequestid, Guid orderid, string refundnumber, decimal approvedamount, string currency, RefundMethod refundmethod, string? refunddestination, string? paymentprovider, DateTime processedat, string? correlationid)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        RefundNumber = refundnumber;
        ApprovedAmount = approvedamount;
        Currency = currency;
        RefundMethod = refundmethod;
        RefundDestination = refunddestination;
        PaymentProvider = paymentprovider;
        ProcessedAt = processedat;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Event raised when a refund request is completed successfully.
/// </summary>
public class RefundRequestCompletedEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public string RefundNumber { get; }
    public decimal RefundedAmount { get; }
    public decimal FeeAmount { get; }
    public decimal NetRefundAmount { get; }
    public string Currency { get; }
    public RefundMethod RefundMethod { get; }
    public string? ExternalRefundId { get; }
    public string? PaymentProvider { get; }
    public DateTime CompletedAt { get; }
    public string? CorrelationId { get; }

    public RefundRequestCompletedEvent(Guid refundrequestid, Guid orderid, string refundnumber, decimal refundedamount, decimal feeamount, decimal netrefundamount, string currency, RefundMethod refundmethod, string? externalrefundid, string? paymentprovider, DateTime completedat, string? correlationid)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        RefundNumber = refundnumber;
        RefundedAmount = refundedamount;
        FeeAmount = feeamount;
        NetRefundAmount = netrefundamount;
        Currency = currency;
        RefundMethod = refundmethod;
        ExternalRefundId = externalrefundid;
        PaymentProvider = paymentprovider;
        CompletedAt = completedat;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Event raised when a refund request fails.
/// </summary>
public class RefundRequestFailedEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public string RefundNumber { get; }
    public decimal RequestedAmount { get; }
    public string Currency { get; }
    public string ErrorMessage { get; }
    public string? ErrorCode { get; }
    public int RetryAttempts { get; }
    public int MaxRetryAttempts { get; }
    public DateTime? NextRetryAt { get; }
    public DateTime FailedAt { get; }
    public string? CorrelationId { get; }

    public RefundRequestFailedEvent(Guid refundrequestid, Guid orderid, string refundnumber, decimal requestedamount, string currency, string errormessage, string? errorcode, int retryattempts, int maxretryattempts, DateTime? nextretryat, DateTime failedat, string? correlationid)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        RefundNumber = refundnumber;
        RequestedAmount = requestedamount;
        Currency = currency;
        ErrorMessage = errormessage;
        ErrorCode = errorcode;
        RetryAttempts = retryattempts;
        MaxRetryAttempts = maxretryattempts;
        NextRetryAt = nextretryat;
        FailedAt = failedat;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Event raised when a refund request is cancelled.
/// </summary>
public class RefundRequestCancelledEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public string RefundNumber { get; }
    public decimal RequestedAmount { get; }
    public string Currency { get; }
    public Guid CancelledByUserId { get; }
    public DateTime CancelledAt { get; }
    public string CancellationReason { get; }
    public string? CorrelationId { get; }

    public RefundRequestCancelledEvent(Guid refundrequestid, Guid orderid, string refundnumber, decimal requestedamount, string currency, Guid cancelledbyuserid, DateTime cancelledat, string cancellationreason, string? correlationid)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        RefundNumber = refundnumber;
        RequestedAmount = requestedamount;
        Currency = currency;
        CancelledByUserId = cancelledbyuserid;
        CancelledAt = cancelledat;
        CancellationReason = cancellationreason;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Event raised when a refund request retry is scheduled.
/// </summary>
public class RefundRequestRetryScheduledEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public string RefundNumber { get; }
    public int CurrentRetryAttempts { get; }
    public int MaxRetryAttempts { get; }
    public DateTime NextRetryAt { get; }
    public string LastErrorMessage { get; }
    public DateTime ScheduledAt { get; }
    public string? CorrelationId { get; }

    public RefundRequestRetryScheduledEvent(Guid refundrequestid, Guid orderid, string refundnumber, int currentretryattempts, int maxretryattempts, DateTime nextretryat, string lasterrormessage, DateTime scheduledat, string? correlationid)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        RefundNumber = refundnumber;
        CurrentRetryAttempts = currentretryattempts;
        MaxRetryAttempts = maxretryattempts;
        NextRetryAt = nextretryat;
        LastErrorMessage = lasterrormessage;
        ScheduledAt = scheduledat;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Event raised when a refund request reaches maximum retry attempts.
/// </summary>
public class RefundRequestMaxRetriesReachedEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public string RefundNumber { get; }
    public decimal RequestedAmount { get; }
    public string Currency { get; }
    public int MaxRetryAttempts { get; }
    public string LastErrorMessage { get; }
    public DateTime MaxRetriesReachedAt { get; }
    public string? CorrelationId { get; }

    public RefundRequestMaxRetriesReachedEvent(Guid refundrequestid, Guid orderid, string refundnumber, decimal requestedamount, string currency, int maxretryattempts, string lasterrormessage, DateTime maxretriesreachedat, string? correlationid)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        RefundNumber = refundnumber;
        RequestedAmount = requestedamount;
        Currency = currency;
        MaxRetryAttempts = maxretryattempts;
        LastErrorMessage = lasterrormessage;
        MaxRetriesReachedAt = maxretriesreachedat;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Event raised when refund line items are added to a refund request.
/// </summary>
public class RefundLineItemsAddedEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public string RefundNumber { get; }
    public List<RefundLineItemInfo> LineItems { get; }
    public decimal TotalRefundAmount { get; }
    public string Currency { get; }
    public DateTime AddedAt { get; }
    public string? CorrelationId { get; }

    public RefundLineItemsAddedEvent(Guid refundrequestid, Guid orderid, string refundnumber, List<RefundLineItemInfo> lineitems, decimal totalrefundamount, string currency, DateTime addedat, string? correlationid)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        RefundNumber = refundnumber;
        LineItems = lineitems;
        TotalRefundAmount = totalrefundamount;
        Currency = currency;
        AddedAt = addedat;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Event raised when a refund line item is marked as returned.
/// </summary>
public class RefundLineItemReturnedEvent : DomainEvent
{
    public Guid RefundLineItemId { get; }
    public Guid RefundRequestId { get; }
    public Guid OrderLineItemId { get; }
    public Guid ProductId { get; }
    public string ProductSku { get; }
    public int RefundQuantity { get; }
    public decimal RefundAmount { get; }
    public string? ReturnCondition { get; }
    public DateTime ReturnedAt { get; }
    public string? CorrelationId { get; }

    public RefundLineItemReturnedEvent(Guid refundlineitemid, Guid refundrequestid, Guid orderlineitemid, Guid productid, string productsku, int refundquantity, decimal refundamount, string? returncondition, DateTime returnedat, string? correlationid)
    {
        RefundLineItemId = refundlineitemid;
        RefundRequestId = refundrequestid;
        OrderLineItemId = orderlineitemid;
        ProductId = productid;
        ProductSku = productsku;
        RefundQuantity = refundquantity;
        RefundAmount = refundamount;
        ReturnCondition = returncondition;
        ReturnedAt = returnedat;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Event raised when a partial refund is processed.
/// </summary>
public class PartialRefundProcessedEvent : DomainEvent
{
    public Guid RefundRequestId { get; }
    public Guid OrderId { get; }
    public string RefundNumber { get; }
    public decimal OriginalRequestedAmount { get; }
    public decimal PartialRefundedAmount { get; }
    public decimal RemainingAmount { get; }
    public string Currency { get; }
    public string PartialRefundReason { get; }
    public DateTime ProcessedAt { get; }
    public string? CorrelationId { get; }

    public PartialRefundProcessedEvent(Guid refundrequestid, Guid orderid, string refundnumber, decimal originalrequestedamount, decimal partialrefundedamount, decimal remainingamount, string currency, string partialrefundreason, DateTime processedat, string? correlationid)
    {
        RefundRequestId = refundrequestid;
        OrderId = orderid;
        RefundNumber = refundnumber;
        OriginalRequestedAmount = originalrequestedamount;
        PartialRefundedAmount = partialrefundedamount;
        RemainingAmount = remainingamount;
        Currency = currency;
        PartialRefundReason = partialrefundreason;
        ProcessedAt = processedat;
        CorrelationId = correlationid;
    }
}

/// <summary>
/// Information about a refund line item for events.
/// </summary>
public class RefundLineItemInfo(
    Guid RefundLineItemId,
    Guid OrderLineItemId,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    int OriginalQuantity,
    int RefundQuantity,
    decimal UnitPrice,
    decimal RefundAmount,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal NetRefundAmount,
    string? ItemRefundReason,
    bool RequiresReturn
);