using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.CommerceModule.Domain.Events;

/// <summary>
/// Event raised when a split payment rule is created.
/// </summary>
public class SplitPaymentRuleCreatedEvent : DomainEvent
{
    public Guid RuleId { get; }
    public string Name { get; }
    public string Description { get; }
    public bool IsActive { get; }
    public DateTime? ExpiryDate { get; }
    public string? ConditionsJson { get; }
    public string SplitConfigurationJson { get; }
    public DateTime CreatedAt { get; }
    public string? CorrelationId { get; }

    public SplitPaymentRuleCreatedEvent(Guid ruleId, string name, string description, bool isActive, DateTime? expiryDate, string? conditionsJson, string splitConfigurationJson, DateTime createdAt, string? correlationId = null)
    {
        RuleId = ruleId;
        Name = name;
        Description = description;
        IsActive = isActive;
        ExpiryDate = expiryDate;
        ConditionsJson = conditionsJson;
        SplitConfigurationJson = splitConfigurationJson;
        CreatedAt = createdAt;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a split payment rule is updated.
/// </summary>
public class SplitPaymentRuleUpdatedEvent : DomainEvent
{
    public Guid RuleId { get; }
    public string Name { get; }
    public string Description { get; }
    public bool IsActive { get; }
    public DateTime? ExpiryDate { get; }
    public string? ConditionsJson { get; }
    public string SplitConfigurationJson { get; }
    public DateTime UpdatedAt { get; }
    public string? CorrelationId { get; }

    public SplitPaymentRuleUpdatedEvent(Guid ruleId, string name, string description, bool isActive, DateTime? expiryDate, string? conditionsJson, string splitConfigurationJson, DateTime updatedAt, string? correlationId = null)
    {
        RuleId = ruleId;
        Name = name;
        Description = description;
        IsActive = isActive;
        ExpiryDate = expiryDate;
        ConditionsJson = conditionsJson;
        SplitConfigurationJson = splitConfigurationJson;
        UpdatedAt = updatedAt;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a split payment rule is deleted.
/// </summary>
public class SplitPaymentRuleDeletedEvent : DomainEvent
{
    public Guid RuleId { get; }
    public string Name { get; }
    public DateTime DeletedAt { get; }
    public string? CorrelationId { get; }

    public SplitPaymentRuleDeletedEvent(Guid ruleId, string name, DateTime deletedAt, string? correlationId = null)
    {
        RuleId = ruleId;
        Name = name;
        DeletedAt = deletedAt;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a split payment is executed.
/// </summary>
public class SplitPaymentExecutedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid RuleId { get; }
    public decimal TotalAmount { get; }
    public string SplitDetailsJson { get; }
    public DateTime ExecutedAt { get; }
    public string? CorrelationId { get; }

    public SplitPaymentExecutedEvent(Guid paymentId, Guid ruleId, decimal totalAmount, string splitDetailsJson, DateTime executedAt, string? correlationId = null)
    {
        PaymentId = paymentId;
        RuleId = ruleId;
        TotalAmount = totalAmount;
        SplitDetailsJson = splitDetailsJson;
        ExecutedAt = executedAt;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a split payment execution fails.
/// </summary>
public class SplitPaymentExecutionFailedEvent : DomainEvent
{
    public Guid PaymentId { get; }
    public Guid RuleId { get; }
    public decimal TotalAmount { get; }
    public string ErrorMessage { get; }
    public string? ErrorDetails { get; }
    public DateTime FailedAt { get; }
    public string? CorrelationId { get; }

    public SplitPaymentExecutionFailedEvent(Guid paymentId, Guid ruleId, decimal totalAmount, string errorMessage, string? errorDetails, DateTime failedAt, string? correlationId = null)
    {
        PaymentId = paymentId;
        RuleId = ruleId;
        TotalAmount = totalAmount;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
        FailedAt = failedAt;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a split payment allocation is created.
/// </summary>
public class SplitPaymentAllocationCreatedEvent : DomainEvent
{
    public Guid AllocationId { get; }
    public Guid PaymentId { get; }
    public Guid RecipientId { get; }
    public decimal Amount { get; }
    public decimal Percentage { get; }
    public string AllocationMethod { get; }
    public DateTime CreatedAt { get; }
    public string? CorrelationId { get; }

    public SplitPaymentAllocationCreatedEvent(Guid allocationId, Guid paymentId, Guid recipientId, decimal amount, decimal percentage, string allocationMethod, DateTime createdAt, string? correlationId = null)
    {
        AllocationId = allocationId;
        PaymentId = paymentId;
        RecipientId = recipientId;
        Amount = amount;
        Percentage = percentage;
        AllocationMethod = allocationMethod;
        CreatedAt = createdAt;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a split payment allocation is processed.
/// </summary>
public class SplitPaymentAllocationProcessedEvent : DomainEvent
{
    public Guid AllocationId { get; }
    public Guid PaymentId { get; }
    public Guid RecipientId { get; }
    public decimal Amount { get; }
    public string Status { get; }
    public string? TransactionReference { get; }
    public DateTime ProcessedAt { get; }
    public string? CorrelationId { get; }

    public SplitPaymentAllocationProcessedEvent(Guid allocationId, Guid paymentId, Guid recipientId, decimal amount, string status, string? transactionReference, DateTime processedAt, string? correlationId = null)
    {
        AllocationId = allocationId;
        PaymentId = paymentId;
        RecipientId = recipientId;
        Amount = amount;
        Status = status;
        TransactionReference = transactionReference;
        ProcessedAt = processedAt;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Event raised when a split payment allocation processing fails.
/// </summary>
public class SplitPaymentAllocationFailedEvent : DomainEvent
{
    public Guid AllocationId { get; }
    public Guid PaymentId { get; }
    public Guid RecipientId { get; }
    public decimal Amount { get; }
    public string ErrorMessage { get; }
    public string? ErrorDetails { get; }
    public DateTime FailedAt { get; }
    public string? CorrelationId { get; }

    public SplitPaymentAllocationFailedEvent(Guid allocationId, Guid paymentId, Guid recipientId, decimal amount, string errorMessage, string? errorDetails, DateTime failedAt, string? correlationId = null)
    {
        AllocationId = allocationId;
        PaymentId = paymentId;
        RecipientId = recipientId;
        Amount = amount;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
        FailedAt = failedAt;
        CorrelationId = correlationId;
    }
}