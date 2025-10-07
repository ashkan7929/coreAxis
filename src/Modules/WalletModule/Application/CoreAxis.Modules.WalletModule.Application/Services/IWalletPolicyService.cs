using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.WalletModule.Application.Services;

public interface IWalletPolicyService
{
    Task<WalletPolicy> GetPolicyAsync(string tenantId, string currency, CancellationToken cancellationToken = default);
}

public class WalletPolicy
{
    public bool AllowNegative { get; init; } = false;
    public decimal? DailyDebitCap { get; init; } = null;
}