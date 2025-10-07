using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.WalletModule.Application.Services;
using CoreAxis.Modules.WalletModule.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Services;

public class WalletPolicyService : IWalletPolicyService
{
    private readonly IOptionsSnapshot<WalletPolicyOptions> _options;

    public WalletPolicyService(IOptionsSnapshot<WalletPolicyOptions> options)
    {
        _options = options;
    }

    public Task<WalletPolicy> GetPolicyAsync(string tenantId, string currency, CancellationToken cancellationToken = default)
    {
        var cfg = _options.Value;
        CurrencyPolicy policy;

        if (!string.IsNullOrWhiteSpace(currency) && cfg.Currencies.TryGetValue(currency, out var byCurrency))
        {
            policy = byCurrency;
        }
        else
        {
            policy = cfg.Default;
        }

        var result = new WalletPolicy
        {
            AllowNegative = policy.AllowNegative,
            DailyDebitCap = policy.DailyDebitCap
        };

        return Task.FromResult(result);
    }
}