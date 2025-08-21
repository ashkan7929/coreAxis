using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.CommerceModule.Domain.Events;

/// <summary>
/// Event raised when a reconciliation process is completed.
/// </summary>
public class ReconciliationCompletedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string PaymentProvider { get; }
    public int TotalTransactions { get; }
    public int MatchedTransactions { get; }
    public int UnmatchedTransactions { get; }
    public decimal TotalAmount { get; }
    public decimal MatchedAmount { get; }
    public decimal UnmatchedAmount { get; }
    public DateTime CompletedAt { get; }
    public string? CorrelationId { get; }

    public ReconciliationCompletedEvent(Guid sessionid, string paymentprovider, int totaltransactions, int matchedtransactions, int unmatchedtransactions, decimal totalamount, decimal matchedamount, decimal unmatchedamount, DateTime completedat, string? correlationId = null)
    {
        SessionId = sessionid;
        PaymentProvider = paymentprovider;
        TotalTransactions = totaltransactions;
        MatchedTransactions = matchedtransactions;
        UnmatchedTransactions = unmatchedtransactions;
        TotalAmount = totalamount;
        MatchedAmount = matchedamount;
        UnmatchedAmount = unmatchedamount;
        CompletedAt = completedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a reconciliation process fails.
/// </summary>
public class ReconciliationFailedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string PaymentProvider { get; }
    public string ErrorMessage { get; }
    public DateTime FailedAt { get; }
    public string? CorrelationId { get; }

    public ReconciliationFailedEvent(Guid sessionid, string paymentprovider, string errormessage, DateTime failedat, string? correlationId = null)
    {
        SessionId = sessionid;
        PaymentProvider = paymentprovider;
        ErrorMessage = errormessage;
        FailedAt = failedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a statement transaction is successfully matched with a payment.
/// </summary>
public class TransactionMatchedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string StatementTransactionId { get; }
    public Guid? PaymentId { get; }
    public Guid? OrderId { get; }
    public decimal Amount { get; }
    public int MatchConfidence { get; }
    public DateTime MatchedAt { get; }
    public string? CorrelationId { get; }

    public TransactionMatchedEvent(Guid sessionid, string statementtransactionid, Guid? paymentid, Guid? orderid, decimal amount, int matchconfidence, DateTime matchedat, string? correlationId = null)
    {
        SessionId = sessionid;
        StatementTransactionId = statementtransactionid;
        PaymentId = paymentid;
        OrderId = orderid;
        Amount = amount;
        MatchConfidence = matchconfidence;
        MatchedAt = matchedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a statement transaction cannot be matched.
/// </summary>
public class TransactionUnmatchedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string StatementTransactionId { get; }
    public decimal Amount { get; }
    public DateTime TransactionDate { get; }
    public string UnmatchedReason { get; }
    public DateTime ProcessedAt { get; }
    public string? CorrelationId { get; }

    public TransactionUnmatchedEvent(Guid sessionid, string statementtransactionid, decimal amount, DateTime transactiondate, string unmatchedreason, DateTime processedat, string? correlationId = null)
    {
        SessionId = sessionid;
        StatementTransactionId = statementtransactionid;
        Amount = amount;
        TransactionDate = transactiondate;
        UnmatchedReason = unmatchedreason;
        ProcessedAt = processedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when an amount discrepancy is detected during reconciliation.
/// </summary>
public class AmountDiscrepancyDetectedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string StatementTransactionId { get; }
    public Guid PaymentId { get; }
    public decimal StatementAmount { get; }
    public decimal SystemAmount { get; }
    public decimal Difference { get; }
    public DateTime DetectedAt { get; }
    public string? CorrelationId { get; }

    public AmountDiscrepancyDetectedEvent(Guid sessionid, string statementtransactionid, Guid paymentid, decimal statementamount, decimal systemamount, decimal difference, DateTime detectedat, string? correlationId = null)
    {
        SessionId = sessionid;
        StatementTransactionId = statementtransactionid;
        PaymentId = paymentid;
        StatementAmount = statementamount;
        SystemAmount = systemamount;
        Difference = difference;
        DetectedAt = detectedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when duplicate transactions are detected in a statement.
/// </summary>
public class DuplicateTransactionDetectedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string StatementTransactionId { get; }
    public string DuplicateTransactionId { get; }
    public decimal Amount { get; }
    public DateTime DetectedAt { get; }
    public string? CorrelationId { get; }

    public DuplicateTransactionDetectedEvent(Guid sessionid, string statementtransactionid, string duplicatetransactionid, decimal amount, DateTime detectedat, string? correlationId = null)
    {
        SessionId = sessionid;
        StatementTransactionId = statementtransactionid;
        DuplicateTransactionId = duplicatetransactionid;
        Amount = amount;
        DetectedAt = detectedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a high number of unmatched transactions is detected.
/// </summary>
public class HighUnmatchedRateDetectedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string PaymentProvider { get; }
    public int TotalTransactions { get; }
    public int UnmatchedTransactions { get; }
    public decimal UnmatchedRate { get; }
    public decimal Threshold { get; }
    public DateTime DetectedAt { get; }
    public string? CorrelationId { get; }

    public HighUnmatchedRateDetectedEvent(Guid sessionid, string paymentprovider, int totaltransactions, int unmatchedtransactions, decimal unmatchedrate, decimal threshold, DateTime detectedat, string? correlationId = null)
    {
        SessionId = sessionid;
        PaymentProvider = paymentprovider;
        TotalTransactions = totaltransactions;
        UnmatchedTransactions = unmatchedtransactions;
        UnmatchedRate = unmatchedrate;
        Threshold = threshold;
        DetectedAt = detectedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a reconciliation session is started.
/// </summary>
public class ReconciliationStartedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string PaymentProvider { get; }
    public DateTime PeriodStart { get; }
    public DateTime PeriodEnd { get; }
    public int ExpectedTransactions { get; }
    public DateTime StartedAt { get; }
    public string? CorrelationId { get; }

    public ReconciliationStartedEvent(Guid sessionid, string paymentprovider, DateTime periodstart, DateTime periodend, int expectedtransactions, DateTime startedat, string? correlationId = null)
    {
        SessionId = sessionid;
        PaymentProvider = paymentprovider;
        PeriodStart = periodstart;
        PeriodEnd = periodend;
        ExpectedTransactions = expectedtransactions;
        StartedAt = startedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when manual intervention is required for reconciliation.
/// </summary>
public class ManualReconciliationRequiredEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string StatementTransactionId { get; }
    public decimal Amount { get; }
    public string Reason { get; }
    public List<PotentialMatchInfo> PotentialMatches { get; }
    public DateTime RequiredAt { get; }
    public string? CorrelationId { get; }

    public ManualReconciliationRequiredEvent(Guid sessionid, string statementtransactionid, decimal amount, string reason, List<PotentialMatchInfo> potentialmatches, DateTime requiredat, string? correlationId = null)
    {
        SessionId = sessionid;
        StatementTransactionId = statementtransactionid;
        Amount = amount;
        Reason = reason;
        PotentialMatches = potentialmatches;
        RequiredAt = requiredat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a reconciliation rule is violated.
/// </summary>
public class ReconciliationRuleViolationEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string RuleName { get; }
    public string ViolationDescription { get; }
    public string StatementTransactionId { get; }
    public Guid? PaymentId { get; }
    public DateTime ViolatedAt { get; }
    public string? CorrelationId { get; }

    public ReconciliationRuleViolationEvent(Guid sessionid, string rulename, string violationdescription, string statementtransactionid, Guid? paymentid, DateTime violatedat, string? correlationId = null)
    {
        SessionId = sessionid;
        RuleName = rulename;
        ViolationDescription = violationdescription;
        StatementTransactionId = statementtransactionid;
        PaymentId = paymentid;
        ViolatedAt = violatedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when reconciliation statistics exceed defined thresholds.
/// </summary>
public class ReconciliationThresholdExceededEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string PaymentProvider { get; }
    public string ThresholdType { get; }
    public decimal ActualValue { get; }
    public decimal ThresholdValue { get; }
    public string Description { get; }
    public DateTime ExceededAt { get; }
    public string? CorrelationId { get; }

    public ReconciliationThresholdExceededEvent(Guid sessionid, string paymentprovider, string thresholdtype, decimal actualvalue, decimal thresholdvalue, string description, DateTime exceededat, string? correlationId = null)
    {
        SessionId = sessionid;
        PaymentProvider = paymentprovider;
        ThresholdType = thresholdtype;
        ActualValue = actualvalue;
        ThresholdValue = thresholdvalue;
        Description = description;
        ExceededAt = exceededat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a payment is automatically corrected during reconciliation.
/// </summary>
public class PaymentAutoCorrectedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public Guid PaymentId { get; }
    public string CorrectionType { get; }
    public decimal OldValue { get; }
    public decimal NewValue { get; }
    public string Reason { get; }
    public DateTime CorrectedAt { get; }
    public string? CorrelationId { get; }

    public PaymentAutoCorrectedEvent(Guid sessionid, Guid paymentid, string correctiontype, decimal oldvalue, decimal newvalue, string reason, DateTime correctedat, string? correlationId = null)
    {
        SessionId = sessionid;
        PaymentId = paymentid;
        CorrectionType = correctiontype;
        OldValue = oldvalue;
        NewValue = newvalue;
        Reason = reason;
        CorrectedAt = correctedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when reconciliation data is archived.
/// </summary>
public class ReconciliationDataArchivedEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string PaymentProvider { get; }
    public DateTime PeriodStart { get; }
    public DateTime PeriodEnd { get; }
    public int ArchivedEntries { get; }
    public DateTime ArchivedAt { get; }
    public string? CorrelationId { get; }

    public ReconciliationDataArchivedEvent(Guid sessionid, string paymentprovider, DateTime periodstart, DateTime periodend, int archivedentries, DateTime archivedat, string? correlationId = null)
    {
        SessionId = sessionid;
        PaymentProvider = paymentprovider;
        PeriodStart = periodstart;
        PeriodEnd = periodend;
        ArchivedEntries = archivedentries;
        ArchivedAt = archivedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Information about potential matches for manual reconciliation.
/// </summary>
public class PotentialMatchInfo(
    Guid PaymentId,
    string OrderNumber,
    decimal Amount,
    DateTime PaymentDate,
    int MatchConfidence,
    string MatchingCriteria
);

/// <summary>
/// Event raised when reconciliation performance metrics are calculated.
/// </summary>
public class ReconciliationMetricsCalculatedEvent : DomainEvent
{
    public string PaymentProvider { get; }
    public DateTime PeriodStart { get; }
    public DateTime PeriodEnd { get; }
    public ReconciliationMetrics Metrics { get; }
    public DateTime CalculatedAt { get; }
    public string? CorrelationId { get; }

    public ReconciliationMetricsCalculatedEvent(string paymentprovider, DateTime periodstart, DateTime periodend, ReconciliationMetrics metrics, DateTime calculatedat, string? correlationId = null)
    {
        PaymentProvider = paymentprovider;
        PeriodStart = periodstart;
        PeriodEnd = periodend;
        Metrics = metrics;
        CalculatedAt = calculatedat;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Reconciliation performance metrics.
/// </summary>
public class ReconciliationMetrics(
    decimal AverageMatchRate,
    TimeSpan AverageProcessingTime,
    int TotalSessionsProcessed,
    int TotalTransactionsProcessed,
    decimal AverageAmountDiscrepancy,
    int ManualInterventionsRequired,
    Dictionary<string, int> UnmatchedReasonBreakdown
);

/// <summary>
/// Event raised when a reconciliation alert is triggered.
/// </summary>
public class ReconciliationAlertTriggeredEvent : DomainEvent
{
    public Guid SessionId { get; }
    public string AlertType { get; }
    public string AlertMessage { get; }
    public string Severity { get; }
    public object AlertData { get; }
    public DateTime TriggeredAt { get; }
    public string? CorrelationId { get; }

    public ReconciliationAlertTriggeredEvent(Guid sessionid, string alerttype, string alertmessage, string severity, object alertdata, DateTime triggeredat, string? correlationId = null)
    {
        SessionId = sessionid;
        AlertType = alerttype;
        AlertMessage = alertmessage;
        Severity = severity;
        AlertData = alertdata;
        TriggeredAt = triggeredat;
        CorrelationId = correlationId;
    }
}