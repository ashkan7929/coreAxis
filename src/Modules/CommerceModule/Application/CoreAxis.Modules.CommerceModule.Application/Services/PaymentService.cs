using CoreAxis.Modules.CommerceModule.Application.Interfaces;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;
using CoreAxis.Modules.CommerceModule.Domain.Events;
using CoreAxis.SharedKernel.Domain;
using CoreAxis.EventBus;
using CoreAxis.SharedKernel.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.CommerceModule.Application.Services;

/// <summary>
/// Service for managing payment operations.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ICommerceDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<PaymentService> _logger;
    private readonly IEventBus _eventBus;

    public PaymentService(
        ICommerceDbContext context,
        IDomainEventDispatcher eventDispatcher,
        ILogger<PaymentService> logger,
        IEventBus eventBus)
    {
        _context = context;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        PaymentRequest request,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing payment for order {OrderId} with amount {Amount}",
                request.OrderId, request.Amount);

            // Implementation would go here
            // This is a placeholder implementation
            await Task.CompletedTask;
            
            return new PaymentResult
            {
                Success = true,
                PaymentId = Guid.NewGuid(),
                TransactionId = Guid.NewGuid().ToString(),
                Status = PaymentStatus.Completed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", request.OrderId);
            throw;
        }
    }

    public async Task<RefundResult> ProcessRefundAsync(
        CoreAxis.Modules.CommerceModule.Application.Interfaces.RefundRequest request,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing refund for payment {PaymentId} with amount {Amount}",
                request.PaymentId, request.Amount);

            // Implementation would go here
            // This is a placeholder implementation
            await Task.CompletedTask;
            
            return new RefundResult
            {
                Success = true,
                RefundId = Guid.NewGuid(),
                TransactionId = Guid.NewGuid().ToString(),
                Status = PaymentStatus.Refunded
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", request.PaymentId);
            throw;
        }
    }

    public async Task<Payment?> GetPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
    }

    public async Task<List<Payment>> GetPaymentsByOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Where(p => p.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentIntent> CreatePaymentIntentAsync(
        CreatePaymentIntentDto request,
        CancellationToken cancellationToken = default)
    {
        var intent = new PaymentIntent
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            Amount = request.Amount,
            Currency = request.Currency,
            Provider = request.Provider,
            CallbackUrl = request.CallbackUrl,
            ReturnUrl = request.ReturnUrl,
            Status = PaymentIntentStatus.Initiated,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        // Mock provider logic
        if (request.Provider == "Mock" || string.IsNullOrEmpty(request.Provider))
        {
            intent.ClientSecret = "mock_secret_" + Guid.NewGuid();
            intent.ExternalId = "mock_ext_" + Guid.NewGuid();
            intent.Status = PaymentIntentStatus.Pending;
        }

        _context.PaymentIntents.Add(intent);
        await _context.SaveChangesAsync(cancellationToken);
        return intent;
    }

    public async Task<PaymentIntent?> GetPaymentIntentAsync(
        Guid intentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PaymentIntents
            .FirstOrDefaultAsync(p => p.Id == intentId, cancellationToken);
    }

    public async Task<PaymentIntent> HandleCallbackAsync(
        string provider,
        string payload,
        Dictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        string externalId = null;
        string statusStr = null;
        
        try 
        {
            var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("externalId", out var extIdProp)) externalId = extIdProp.GetString();
            if (doc.RootElement.TryGetProperty("status", out var statusProp)) statusStr = statusProp.GetString();
        }
        catch { /* ignore parse error */ }

        if (string.IsNullOrEmpty(externalId))
        {
            throw new ArgumentException("Invalid payload: missing externalId");
        }

        var intent = await _context.PaymentIntents
            .FirstOrDefaultAsync(p => p.ExternalId == externalId, cancellationToken);

        if (intent == null)
        {
            throw new KeyNotFoundException($"PaymentIntent with externalId {externalId} not found");
        }

        if (intent.Status == PaymentIntentStatus.Paid || intent.Status == PaymentIntentStatus.Failed)
        {
            return intent;
        }

        if (statusStr == "paid" || statusStr == "succeeded")
        {
            intent.Status = PaymentIntentStatus.Paid;
            await _eventBus.PublishAsync(new PaymentConfirmed(
                intent.OrderId, 
                intent.Id, 
                intent.Id, 
                intent.Amount, 
                intent.Currency, 
                DateTime.UtcNow, 
                "default", 
                intent.OrderId));
        }
        else if (statusStr == "failed")
        {
            intent.Status = PaymentIntentStatus.Failed;
            await _eventBus.PublishAsync(new PaymentFailed(
                intent.OrderId, 
                intent.Id, 
                "Provider reported failure", 
                "default", 
                intent.OrderId));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return intent;
    }
}