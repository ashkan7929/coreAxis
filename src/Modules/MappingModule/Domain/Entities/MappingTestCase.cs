using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.MappingModule.Domain.Entities;

public class MappingTestCase : EntityBase
{
    public Guid MappingDefinitionId { get; set; }
    public string InputContextJson { get; set; } = "{}";
    public string ExpectedOutputJson { get; set; } = "{}";

    public MappingDefinition MappingDefinition { get; set; } = null!;
}
