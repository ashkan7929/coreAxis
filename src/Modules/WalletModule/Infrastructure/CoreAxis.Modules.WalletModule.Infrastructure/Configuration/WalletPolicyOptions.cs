using System.Collections.Generic;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Configuration;

public class WalletPolicyOptions
{
    public CurrencyPolicy Default { get; set; } = new();
    public Dictionary<string, CurrencyPolicy> Currencies { get; set; } = new();
}

public class CurrencyPolicy
{
    public bool AllowNegative { get; set; } = false;
    public decimal? DailyDebitCap { get; set; } = null;
}