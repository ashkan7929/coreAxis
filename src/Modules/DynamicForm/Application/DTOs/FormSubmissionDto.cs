namespace CoreAxis.Modules.DynamicForm.Application.DTOs;

public class FormSubmissionDto
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    public Dictionary<string, object> SubmissionData { get; set; } = new();
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Status { get; set; } = "Submitted";
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public FormDto? Form { get; set; }
}

public class ValidationResultDto
{
    public bool IsValid { get; set; }
    public ICollection<ValidationErrorDto> Errors { get; set; } = new List<ValidationErrorDto>();
    public ICollection<ValidationWarningDto> Warnings { get; set; } = new List<ValidationWarningDto>();
    public ICollection<FieldValidationResultDto> FieldResults { get; set; } = new List<FieldValidationResultDto>();
    public ValidationMetricsDto? Metrics { get; set; }
}

public class ValidationErrorDto
{
    public string FieldName { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
    public Dictionary<string, object>? Context { get; set; }
}

public class ValidationWarningDto
{
    public string FieldName { get; set; } = string.Empty;
    public string WarningCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
    public Dictionary<string, object>? Context { get; set; }
}

public class FieldValidationResultDto
{
    public string FieldName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public ICollection<ValidationErrorDto> Errors { get; set; } = new List<ValidationErrorDto>();
    public ICollection<ValidationWarningDto> Warnings { get; set; } = new List<ValidationWarningDto>();
    public object? ValidatedValue { get; set; }
}

public class ValidationMetricsDto
{
    public TimeSpan ValidationDuration { get; set; }
    public int TotalFieldsValidated { get; set; }
    public int FieldsWithErrors { get; set; }
    public int FieldsWithWarnings { get; set; }
    public int RulesEvaluated { get; set; }
    public Dictionary<string, object>? AdditionalMetrics { get; set; }
}

public class SubmissionStatsDto
{
    public Guid FormId { get; set; }
    public int TotalSubmissions { get; set; }
    public int SubmissionsToday { get; set; }
    public int SubmissionsThisWeek { get; set; }
    public int SubmissionsThisMonth { get; set; }
    public DateTime? FirstSubmissionDate { get; set; }
    public DateTime? LastSubmissionDate { get; set; }
    public Dictionary<string, int>? SubmissionsByStatus { get; set; }
    public Dictionary<string, object>? AdditionalStats { get; set; }
}