using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Domain;

namespace CoreAxis.Modules.MappingModule.Domain.Entities;

public class MappingSet : EntityBase, IMustHaveTenant
{
    public string TenantId { get; set; } = "default";
    public string Name { get; set; } = null!;
    public string ItemsJson { get; set; } = "[]"; // List of mapping definition IDs or embedded rules
}
