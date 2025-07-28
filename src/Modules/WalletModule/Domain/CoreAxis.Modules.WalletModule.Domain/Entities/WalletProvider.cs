using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.WalletModule.Domain.Entities;

public class WalletProvider : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty; // Bank, Gateway, etc.
    public string? ApiUrl { get; private set; }
    public bool SupportsDeposit { get; private set; } = true;
    public bool SupportsWithdraw { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public string? Configuration { get; private set; } // JSON configuration

    // Navigation properties
    public virtual ICollection<WalletContract> WalletContracts { get; private set; } = new List<WalletContract>();

    private WalletProvider() { } // For EF Core

    public WalletProvider(
        string name, 
        string type, 
        Guid tenantId,
        string? apiUrl = null,
        bool supportsDeposit = true,
        bool supportsWithdraw = true,
        string? configuration = null)
    {
        Name = name;
        Type = type;
        ApiUrl = apiUrl;
        SupportsDeposit = supportsDeposit;
        SupportsWithdraw = supportsWithdraw;
        Configuration = configuration;
        TenantId = tenantId;
        CreatedOn = DateTime.UtcNow;
    }

    public void UpdateConfiguration(
        string name, 
        string type, 
        string? apiUrl = null,
        bool? supportsDeposit = null,
        bool? supportsWithdraw = null,
        string? configuration = null)
    {
        Name = name;
        Type = type;
        
        if (apiUrl != null) ApiUrl = apiUrl;
        if (supportsDeposit.HasValue) SupportsDeposit = supportsDeposit.Value;
        if (supportsWithdraw.HasValue) SupportsWithdraw = supportsWithdraw.Value;
        if (configuration != null) Configuration = configuration;
        
        LastModifiedOn = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOn = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        LastModifiedOn = DateTime.UtcNow;
    }
}