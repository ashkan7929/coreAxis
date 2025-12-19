using CoreAxis.SharedKernel.Versioning;

namespace CoreAxis.Modules.ProductBuilderModule.Application.DTOs;

public class ProductVersionDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string VersionNumber { get; set; } = default!;
    public VersionStatus Status { get; set; }
    public string? Changelog { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public ProductBindingDto? Binding { get; set; }
}
