using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Versioning;

namespace CoreAxis.Modules.MappingModule.Domain.Entities;

public class MappingDefinition : EntityBase
{
    public string TenantId { get; set; } = "default";
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Version { get; set; } = 1;
    public string? SourceSchemaRef { get; set; }
    public string? TargetSchemaRef { get; set; }
    public string RulesJson { get; set; } = "[]";
    public VersionStatus Status { get; set; } = VersionStatus.Draft;
    public DateTime? PublishedAt { get; set; }

    // Navigation properties can be added if needed, e.g., to TestCases
    public ICollection<MappingTestCase> TestCases { get; set; } = new List<MappingTestCase>();
}
