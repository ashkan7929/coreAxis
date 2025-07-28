namespace CoreAxis.Modules.WalletModule.Application.DTOs;

public class WalletDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid WalletTypeId { get; set; }
    public string WalletTypeName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class CreateWalletDto
{
    public Guid UserId { get; set; }
    public Guid WalletTypeId { get; set; }
    public string Currency { get; set; } = "USD";
}

public class WalletBalanceDto
{
    public Guid WalletId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}