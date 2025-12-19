using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Versioning;

namespace CoreAxis.Modules.ProductBuilderModule.Domain.Entities;

public class ProductVersion : EntityBase
{
    public Guid ProductId { get; set; }
    public ProductDefinition Product { get; set; } = default!;
    public string VersionNumber { get; set; } = default!; // e.g., "1.0.0"
    public VersionStatus Status { get; set; }
    public string? Changelog { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    
    // Bindings
    public ProductBinding? Binding { get; set; }
}
