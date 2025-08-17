namespace CoreAxis.Modules.DynamicForm.Application.DTOs;

public class FormDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SchemaJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? TenantId { get; set; }
    public string? BusinessId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int Version { get; set; }
    
    // Navigation properties
    public ICollection<FormFieldDto>? Fields { get; set; }
    public ICollection<FormSubmissionDto>? Submissions { get; set; }
}

public class FormFieldDto
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, object>? ValidationRules { get; set; }
    public Dictionary<string, object>? ConditionalLogic { get; set; }
    public Dictionary<string, object>? Options { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class FormSchemaDto
{
    public Guid FormId { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string SchemaJson { get; set; } = string.Empty;
    public ICollection<FormFieldDto> Fields { get; set; } = new List<FormFieldDto>();
    public Dictionary<string, object>? ValidationRules { get; set; }
    public Dictionary<string, object>? Dependencies { get; set; }
    public int Version { get; set; }
}