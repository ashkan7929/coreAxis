namespace CoreAxis.Modules.CommerceModule.Domain.Enums;

/// <summary>
/// Represents the status of a payment.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending processing.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Payment has been successfully processed.
    /// </summary>
    Completed = 1,
    
    /// <summary>
    /// Payment has failed.
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Payment has been cancelled.
    /// </summary>
    Cancelled = 3,
    
    /// <summary>
    /// Payment has been refunded.
    /// </summary>
    Refunded = 4,
    
    /// <summary>
    /// Payment is being processed.
    /// </summary>
    Processing = 5,
    
    /// <summary>
    /// Payment requires additional authorization.
    /// </summary>
    RequiresAuthorization = 6,
    
    /// <summary>
    /// Payment has been partially refunded.
    /// </summary>
    PartiallyRefunded = 7
}

/// <summary>
/// Represents the method used for payment.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Credit card payment.
    /// </summary>
    CreditCard = 0,
    
    /// <summary>
    /// Debit card payment.
    /// </summary>
    DebitCard = 1,
    
    /// <summary>
    /// Bank transfer payment.
    /// </summary>
    BankTransfer = 2,
    
    /// <summary>
    /// Digital wallet payment (e.g., PayPal, Apple Pay).
    /// </summary>
    DigitalWallet = 3,
    
    /// <summary>
    /// Cryptocurrency payment.
    /// </summary>
    Cryptocurrency = 4,
    
    /// <summary>
    /// Cash payment.
    /// </summary>
    Cash = 5,
    
    /// <summary>
    /// Check payment.
    /// </summary>
    Check = 6
}