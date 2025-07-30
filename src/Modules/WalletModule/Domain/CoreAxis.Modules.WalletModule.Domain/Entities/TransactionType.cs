using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.WalletModule.Domain.Entities;

public class TransactionType : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public virtual ICollection<Transaction> Transactions { get; private set; } = new List<Transaction>();

    private TransactionType() { } // For EF Core

    public TransactionType(string name, string description, string code)
    {
        Name = name;
        Description = description;
        Code = code;
        CreatedOn = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string description, string code)
    {
        Name = name;
        Description = description;
        Code = code;
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