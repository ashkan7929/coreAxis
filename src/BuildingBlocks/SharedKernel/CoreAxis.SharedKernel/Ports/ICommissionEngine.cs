namespace CoreAxis.SharedKernel.Ports;

public interface ICommissionEngine
{
    Task<CommissionResult> CalculateAsync(PaymentContext paymentContext, CancellationToken cancellationToken = default);
}

public class PaymentContext
{
    public Guid UserId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string PaymentType { get; }
    public string TenantId { get; }
    public Dictionary<string, object> Metadata { get; }

    public PaymentContext(Guid userId, decimal amount, string currency, string paymentType, string tenantId, Dictionary<string, object>? metadata = null)
    {
        UserId = userId;
        Amount = amount;
        Currency = currency;
        PaymentType = paymentType;
        TenantId = tenantId;
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}

public class CommissionResult
{
    public decimal OriginalAmount { get; }
    public decimal CommissionAmount { get; }
    public decimal NetAmount { get; }
    public decimal CommissionRate { get; }
    public string CalculationMethod { get; }
    public Dictionary<string, object> Metadata { get; }
    public DateTime CalculatedAt { get; }

    public CommissionResult(decimal originalAmount, decimal commissionAmount, decimal netAmount, decimal commissionRate, string calculationMethod, Dictionary<string, object>? metadata = null)
    {
        OriginalAmount = originalAmount;
        CommissionAmount = commissionAmount;
        NetAmount = netAmount;
        CommissionRate = commissionRate;
        CalculationMethod = calculationMethod;
        Metadata = metadata ?? new Dictionary<string, object>();
        CalculatedAt = DateTime.UtcNow;
    }
}