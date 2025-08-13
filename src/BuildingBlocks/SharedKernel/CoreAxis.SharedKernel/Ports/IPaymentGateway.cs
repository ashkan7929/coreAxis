namespace CoreAxis.SharedKernel.Ports;

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVerificationResult> VerifyAsync(string referenceId, CancellationToken cancellationToken = default);
}

public class PaymentRequest
{
    public decimal Amount { get; }
    public string Currency { get; }
    public string PaymentMethod { get; }
    public Guid UserId { get; }
    public string IdempotencyKey { get; }
    public Dictionary<string, object> Metadata { get; }

    public PaymentRequest(decimal amount, string currency, string paymentMethod, Guid userId, string idempotencyKey, Dictionary<string, object>? metadata = null)
    {
        Amount = amount;
        Currency = currency;
        PaymentMethod = paymentMethod;
        UserId = userId;
        IdempotencyKey = idempotencyKey;
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}

public class PaymentResult
{
    public string ReferenceId { get; }
    public string Status { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string GatewayResponse { get; }
    public string? TransactionId { get; }
    public DateTime Timestamp { get; }
    public bool IsSuccess { get; }

    public PaymentResult(string referenceId, string status, decimal amount, string currency, string gatewayResponse, string? transactionId, DateTime timestamp, bool isSuccess)
    {
        ReferenceId = referenceId;
        Status = status;
        Amount = amount;
        Currency = currency;
        GatewayResponse = gatewayResponse;
        TransactionId = transactionId;
        Timestamp = timestamp;
        IsSuccess = isSuccess;
    }
}

public class PaymentVerificationResult
{
    public string ReferenceId { get; }
    public bool IsVerified { get; }
    public string Status { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string? TransactionId { get; }
    public DateTime VerificationTimestamp { get; }
    public DateTime? OriginalTimestamp { get; }

    public PaymentVerificationResult(string referenceId, bool isVerified, string status, decimal amount, string currency, string? transactionId, DateTime verificationTimestamp, DateTime? originalTimestamp)
    {
        ReferenceId = referenceId;
        IsVerified = isVerified;
        Status = status;
        Amount = amount;
        Currency = currency;
        TransactionId = transactionId;
        VerificationTimestamp = verificationTimestamp;
        OriginalTimestamp = originalTimestamp;
    }
}