using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.WalletModule.Domain.Entities;

public class WalletType : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsDefault { get; private set; } = false;

    // Navigation properties
    public virtual ICollection<Wallet> Wallets { get; private set; } = new List<Wallet>();

    private WalletType() { } // For EF Core

    public WalletType(string name, string description, bool isDefault = false)
    {
        Name = name;
        Description = description;
        IsDefault = isDefault;
        CreatedOn = DateTime.UtcNow;
        CreatedBy = "System";
        LastModifiedBy = "System";
    }

    public void UpdateDetails(string name, string description, bool? isDefault = null)
    {
        Name = name;
        Description = description;
        if (isDefault.HasValue)
        {
            IsDefault = isDefault.Value;
        }
        LastModifiedOn = DateTime.UtcNow;
        LastModifiedBy = "System";
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOn = DateTime.UtcNow;
        LastModifiedBy = "System";
    }

    public void Activate()
    {
        IsActive = true;
        LastModifiedOn = DateTime.UtcNow;
        LastModifiedBy = "System";
    }

    // Methods to set audit fields for existing records
    public void SetCreatedBy(string createdBy)
    {
        CreatedBy = createdBy;
    }

    public void SetLastModifiedBy(string lastModifiedBy)
    {
        LastModifiedBy = lastModifiedBy;
    }
}