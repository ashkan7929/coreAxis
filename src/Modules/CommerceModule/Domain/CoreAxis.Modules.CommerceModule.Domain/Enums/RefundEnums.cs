namespace CoreAxis.Modules.CommerceModule.Domain.Enums;

/// <summary>
/// Represents the type of refund.
/// </summary>
public enum RefundType
{
    /// <summary>
    /// Full refund of the entire order.
    /// </summary>
    Full = 0,
    
    /// <summary>
    /// Partial refund of specific items or amounts.
    /// </summary>
    Partial = 1,
    
    /// <summary>
    /// Restocking fee refund.
    /// </summary>
    RestockingFee = 2,
    
    /// <summary>
    /// Shipping cost refund.
    /// </summary>
    Shipping = 3,
    
    /// <summary>
    /// Tax refund only.
    /// </summary>
    Tax = 4
}

/// <summary>
/// Represents the reason for a refund.
/// </summary>
public enum RefundReason
{
    /// <summary>
    /// Customer requested cancellation.
    /// </summary>
    CustomerRequest = 0,
    
    /// <summary>
    /// Product was defective or damaged.
    /// </summary>
    Defective = 1,
    
    /// <summary>
    /// Wrong item was shipped.
    /// </summary>
    WrongItem = 2,
    
    /// <summary>
    /// Item was not as described.
    /// </summary>
    NotAsDescribed = 3,
    
    /// <summary>
    /// Item arrived late.
    /// </summary>
    LateDelivery = 4,
    
    /// <summary>
    /// Item was lost in shipping.
    /// </summary>
    LostInShipping = 5,
    
    /// <summary>
    /// Duplicate order or payment.
    /// </summary>
    Duplicate = 6,
    
    /// <summary>
    /// Fraudulent transaction.
    /// </summary>
    Fraud = 7,
    
    /// <summary>
    /// Payment processing error.
    /// </summary>
    ProcessingError = 8,
    
    /// <summary>
    /// Merchant error.
    /// </summary>
    MerchantError = 9,
    
    /// <summary>
    /// Chargeback or dispute.
    /// </summary>
    Chargeback = 10,
    
    /// <summary>
    /// Goodwill gesture.
    /// </summary>
    Goodwill = 11,
    
    /// <summary>
    /// Other reason not listed.
    /// </summary>
    Other = 99
}

/// <summary>
/// Represents the current status of a refund request.
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// Refund request is pending review.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Refund request has been approved.
    /// </summary>
    Approved = 1,
    
    /// <summary>
    /// Refund request has been rejected.
    /// </summary>
    Rejected = 2,
    
    /// <summary>
    /// Refund is being processed.
    /// </summary>
    Processing = 3,
    
    /// <summary>
    /// Refund has been completed successfully.
    /// </summary>
    Completed = 4,
    
    /// <summary>
    /// Refund processing failed.
    /// </summary>
    Failed = 5,
    
    /// <summary>
    /// Refund request was cancelled.
    /// </summary>
    Cancelled = 6,
    
    /// <summary>
    /// Refund is on hold pending additional information.
    /// </summary>
    OnHold = 7,
    
    /// <summary>
    /// Partial refund has been processed.
    /// </summary>
    PartiallyCompleted = 8
}

/// <summary>
/// Represents the method used for processing the refund.
/// </summary>
public enum RefundMethod
{
    /// <summary>
    /// Refund to original payment method.
    /// </summary>
    OriginalPaymentMethod = 0,
    
    /// <summary>
    /// Refund to store credit or wallet.
    /// </summary>
    StoreCredit = 1,
    
    /// <summary>
    /// Refund via bank transfer.
    /// </summary>
    BankTransfer = 2,
    
    /// <summary>
    /// Refund via check.
    /// </summary>
    Check = 3,
    
    /// <summary>
    /// Refund via cash.
    /// </summary>
    Cash = 4,
    
    /// <summary>
    /// Refund via digital wallet (PayPal, etc.).
    /// </summary>
    DigitalWallet = 5,
    
    /// <summary>
    /// Refund via cryptocurrency.
    /// </summary>
    Cryptocurrency = 6,
    
    /// <summary>
    /// Refund via gift card.
    /// </summary>
    GiftCard = 7,
    
    /// <summary>
    /// Other refund method.
    /// </summary>
    Other = 99
}

/// <summary>
/// Represents the priority level of a refund request.
/// </summary>
public enum RefundPriority
{
    /// <summary>
    /// Low priority refund.
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// Normal priority refund.
    /// </summary>
    Normal = 1,
    
    /// <summary>
    /// High priority refund.
    /// </summary>
    High = 2,
    
    /// <summary>
    /// Urgent refund requiring immediate attention.
    /// </summary>
    Urgent = 3,
    
    /// <summary>
    /// Critical refund (fraud, legal issues, etc.).
    /// </summary>
    Critical = 4
}

/// <summary>
/// Represents the approval workflow for refunds.
/// </summary>
public enum RefundApprovalWorkflow
{
    /// <summary>
    /// Automatic approval (no manual review required).
    /// </summary>
    Automatic = 0,
    
    /// <summary>
    /// Single level approval required.
    /// </summary>
    SingleApproval = 1,
    
    /// <summary>
    /// Multi-level approval required.
    /// </summary>
    MultiLevelApproval = 2,
    
    /// <summary>
    /// Manager approval required.
    /// </summary>
    ManagerApproval = 3,
    
    /// <summary>
    /// Finance team approval required.
    /// </summary>
    FinanceApproval = 4
}