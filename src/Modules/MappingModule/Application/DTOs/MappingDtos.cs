using System;
using CoreAxis.SharedKernel.Versioning;

namespace CoreAxis.Modules.MappingModule.Application.DTOs;

/// <summary>
/// Data Transfer Object for Mapping Definition.
/// </summary>
public class MappingDefinitionDto
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the mapping.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the source schema.
    /// </summary>
    public string? SourceSchemaRef { get; set; }

    /// <summary>
    /// Reference to the target schema.
    /// </summary>
    public string? TargetSchemaRef { get; set; }

    /// <summary>
    /// JSON string containing mapping rules.
    /// </summary>
    public string RulesJson { get; set; } = "[]";

    /// <summary>
    /// Status of the mapping version.
    /// </summary>
    public VersionStatus Status { get; set; }

    /// <summary>
    /// Creation date.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Date when the mapping was published.
    /// </summary>
    public DateTime? PublishedAt { get; set; }
}

/// <summary>
/// DTO for creating a new mapping definition.
/// </summary>
public class CreateMappingDefinitionDto
{
    /// <summary>
    /// Name of the mapping.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the source schema.
    /// </summary>
    public string? SourceSchemaRef { get; set; }

    /// <summary>
    /// Reference to the target schema.
    /// </summary>
    public string? TargetSchemaRef { get; set; }

    /// <summary>
    /// JSON string containing mapping rules.
    /// </summary>
    public string RulesJson { get; set; } = "[]";
}

/// <summary>
/// DTO for updating an existing mapping definition.
/// </summary>
public class UpdateMappingDefinitionDto
{
    /// <summary>
    /// Name of the mapping.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Reference to the source schema.
    /// </summary>
    public string? SourceSchemaRef { get; set; }

    /// <summary>
    /// Reference to the target schema.
    /// </summary>
    public string? TargetSchemaRef { get; set; }

    /// <summary>
    /// JSON string containing mapping rules.
    /// </summary>
    public string? RulesJson { get; set; }
}

/// <summary>
/// Request DTO for testing a mapping.
/// </summary>
public class TestMappingRequestDto
{
    /// <summary>
    /// The input context JSON for testing.
    /// </summary>
    public string ContextJson { get; set; } = "{}";
}

/// <summary>
/// Response DTO for mapping test execution.
/// </summary>
public class TestMappingResponseDto
{
    /// <summary>
    /// The resulting output JSON.
    /// </summary>
    public string OutputJson { get; set; } = "{}";

    /// <summary>
    /// Indicates whether the execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Data Transfer Object for Mapping Set.
/// </summary>
public class MappingSetDto
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the mapping set.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// JSON string containing items in the set.
    /// </summary>
    public string ItemsJson { get; set; } = "[]";

    /// <summary>
    /// Creation date.
    /// </summary>
    public DateTime CreatedOn { get; set; }
}

/// <summary>
/// DTO for creating a new mapping set.
/// </summary>
public class CreateMappingSetDto
{
    /// <summary>
    /// Name of the mapping set.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// JSON string containing items in the set.
    /// </summary>
    public string ItemsJson { get; set; } = "[]";
}

/// <summary>
/// DTO for updating an existing mapping set.
/// </summary>
public class UpdateMappingSetDto
{
    /// <summary>
    /// Name of the mapping set.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// JSON string containing items in the set.
    /// </summary>
    public string? ItemsJson { get; set; }
}