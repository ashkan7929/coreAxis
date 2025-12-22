using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Domain;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.SecretsModule.Domain.Entities;

public class Secret : EntityBase, IMustHaveTenant
{
    [Required]
    [MaxLength(100)]
    public string Key { get; private set; } = string.Empty;

    [Required]
    public string EncryptedValue { get; private set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; private set; }

    [Required]
    [MaxLength(100)]
    public string TenantId { get; set; } = "default";

    public Secret(string key, string encryptedValue, string tenantId, string? description = null)
    {
        Key = key;
        EncryptedValue = encryptedValue;
        TenantId = tenantId;
        Description = description;
    }

    public void UpdateValue(string newEncryptedValue)
    {
        EncryptedValue = newEncryptedValue;
        LastModifiedOn = DateTime.UtcNow;
    }

    // Protected constructor for EF
    protected Secret() { }
}
