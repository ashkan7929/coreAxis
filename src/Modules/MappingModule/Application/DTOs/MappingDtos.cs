using CoreAxis.SharedKernel.Versioning;

namespace CoreAxis.Modules.MappingModule.Application.DTOs;

public class MappingDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SourceSchemaRef { get; set; }
    public string? TargetSchemaRef { get; set; }
    public string RulesJson { get; set; } = "[]";
    public VersionStatus Status { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class CreateMappingDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public string? SourceSchemaRef { get; set; }
    public string? TargetSchemaRef { get; set; }
    public string RulesJson { get; set; } = "[]";
}

public class UpdateMappingDefinitionDto
{
    public string? Name { get; set; }
    public string? SourceSchemaRef { get; set; }
    public string? TargetSchemaRef { get; set; }
    public string? RulesJson { get; set; }
}

public class TestMappingRequestDto
{
    public string ContextJson { get; set; } = "{}";
}

public class TestMappingResponseDto
{
    public string OutputJson { get; set; } = "{}";
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class MappingSetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ItemsJson { get; set; } = "[]";
    public DateTime CreatedOn { get; set; }
}

public class CreateMappingSetDto
{
    public string Name { get; set; } = string.Empty;
    public string ItemsJson { get; set; } = "[]";
}

public class UpdateMappingSetDto
{
    public string? Name { get; set; }
    public string? ItemsJson { get; set; }
}
