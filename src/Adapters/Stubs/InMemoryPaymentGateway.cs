using CoreAxis.SharedKernel.Ports;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CoreAxis.Adapters.Stubs;

public class InMemoryPaymentGateway : IPaymentGateway
{
    private readonly ILogger<InMemoryPaymentGateway> _logger;
    private readonly ConcurrentDictionary<string, PaymentRecord> _payments = new();
    private readonly Random _random = new();

    public InMemoryPaymentGateway(ILogger<InMemoryPaymentGateway> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken); // Simulate network call

        var referenceId = Guid.NewGuid().ToString("N")[..16].ToUpper();
        
        // Simulate random failures (5% chance)
        var shouldFail = _random.NextDouble() < 0.05;
        
        PaymentResult result;
        
        if (shouldFail)
        {
            result = new PaymentResult(
                referenceId: referenceId,
                status: "Failed",
                amount: request.Amount,
                currency: request.Currency,
                gatewayResponse: "Insufficient funds or card declined",
                transactionId: null,
                timestamp: DateTime.UtcNow,
                isSuccess: false
            );
            
            _logger.LogWarning("Payment failed for amount {Amount} {Currency} - Reference: {ReferenceId}", 
                request.Amount, request.Currency, referenceId);
        }
        else
        {
            var transactionId = Guid.NewGuid().ToString();
            
            result = new PaymentResult(
                referenceId: referenceId,
                status: "Success",
                amount: request.Amount,
                currency: request.Currency,
                gatewayResponse: "Payment processed successfully",
                transactionId: transactionId,
                timestamp: DateTime.UtcNow,
                isSuccess: true
            );
            
            // Store payment record
            _payments[referenceId] = new PaymentRecord
            {
                ReferenceId = referenceId,
                TransactionId = transactionId,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = "Success",
                CreatedAt = DateTime.UtcNow,
                Request = request
            };
            
            _logger.LogInformation("Payment successful for amount {Amount} {Currency} - Reference: {ReferenceId}, Transaction: {TransactionId}", 
                request.Amount, request.Currency, referenceId, transactionId);
        }

        return result;
    }

    public async Task<PaymentVerificationResult> VerifyAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // Simulate network call

        if (_payments.TryGetValue(referenceId, out var payment))
        {
            var result = new PaymentVerificationResult(
                referenceId: referenceId,
                isVerified: true,
                status: payment.Status,
                amount: payment.Amount,
                currency: payment.Currency,
                transactionId: payment.TransactionId,
                verificationTimestamp: DateTime.UtcNow,
                originalTimestamp: payment.CreatedAt
            );
            
            _logger.LogInformation("Payment verification successful for reference {ReferenceId}", referenceId);
            return result;
        }
        else
        {
            var result = new PaymentVerificationResult(
                referenceId: referenceId,
                isVerified: false,
                status: "NotFound",
                amount: 0,
                currency: "USD",
                transactionId: null,
                verificationTimestamp: DateTime.UtcNow,
                originalTimestamp: null
            );
            
            _logger.LogWarning("Payment verification failed - reference {ReferenceId} not found", referenceId);
            return result;
        }
    }

    private class PaymentRecord
    {
        public string ReferenceId { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public PaymentRequest Request { get; set; } = null!;
    }
}