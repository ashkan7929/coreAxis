namespace CoreAxis.Modules.WalletModule.Infrastructure.Providers;

public class BalanceSnapshot
{
    public Guid WalletId { get; set; }
    public decimal Balance { get; set; }
    public DateTime CapturedAt { get; set; }
}