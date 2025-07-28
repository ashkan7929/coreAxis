using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.WalletModule.Domain.Entities;

public class WalletType : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public virtual ICollection<Wallet> Wallets { get; private set; } = new List<Wallet>();

    private WalletType() { } // For EF Core

    public WalletType(string name, string description, Guid tenantId)
    {
        Name = name;
        Description = description;
        TenantId = tenantId;
        CreatedOn = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string description)
    {
        Name = name;
        Description = description;
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