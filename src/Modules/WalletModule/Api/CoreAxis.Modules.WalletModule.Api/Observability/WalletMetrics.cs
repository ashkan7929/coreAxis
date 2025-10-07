using System.Diagnostics.Metrics;

namespace CoreAxis.Modules.WalletModule.Api.Observability;

public static class WalletMetrics
{
    public static readonly Meter Meter = new("CoreAxis.Wallet");

    public static readonly Counter<long> Deposits = Meter.CreateCounter<long>("wallet_deposits_total");
    public static readonly Counter<long> Withdrawals = Meter.CreateCounter<long>("wallet_withdrawals_total");
    public static readonly Counter<long> Transfers = Meter.CreateCounter<long>("wallet_transfers_total");
    public static readonly Counter<long> Failures = Meter.CreateCounter<long>("wallet_failures_total");

    public static readonly Histogram<double> DepositLatencyMs = Meter.CreateHistogram<double>("wallet_deposit_latency_ms");
    public static readonly Histogram<double> WithdrawLatencyMs = Meter.CreateHistogram<double>("wallet_withdraw_latency_ms");
    public static readonly Histogram<double> TransferLatencyMs = Meter.CreateHistogram<double>("wallet_transfer_latency_ms");
}