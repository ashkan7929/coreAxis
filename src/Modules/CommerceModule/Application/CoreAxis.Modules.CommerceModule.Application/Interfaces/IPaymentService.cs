using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.Application.Interfaces;

/// <summary>
/// Interface for payment service operations.
/// </summary>
public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(
        PaymentRequest request,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<RefundResult> ProcessRefundAsync(
        RefundRequest request,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<List<Payment>> GetPaymentsByOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<PaymentIntent> CreatePaymentIntentAsync(
        CreatePaymentIntentDto request,
        CancellationToken cancellationToken = default);

    Task<PaymentIntent?> GetPaymentIntentAsync(
        Guid intentId,
        CancellationToken cancellationToken = default);

    Task<PaymentIntent> HandleCallbackAsync(
        string provider,
        string payload,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default);
}

public class CreatePaymentIntentDto
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public required string Provider { get; set; }
    public required string CallbackUrl { get; set; }
    public string? ReturnUrl { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request model for payment processing.
/// </summary>
public class PaymentRequest
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentMethod Method { get; set; }
    public string? PaymentMethodDetails { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request model for refund processing.
/// </summary>
public class RefundRequest
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public RefundReason Reason { get; set; }
    public string? ReasonDescription { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Result model for refund processing.
/// </summary>
public class RefundResult
{
    public bool Success { get; set; }
    public Guid? RefundId { get; set; }
    public string? TransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Result model for payment processing.
/// </summary>
public class PaymentResult
{
    public bool Success { get; set; }
    public Guid? PaymentId { get; set; }
    public string? TransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}