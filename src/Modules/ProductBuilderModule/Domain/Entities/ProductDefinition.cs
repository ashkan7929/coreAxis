using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ProductBuilderModule.Domain.Entities;

public class ProductDefinition : EntityBase
{
    public string TenantId { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
