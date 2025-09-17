namespace CoreAxis.Modules.ApiManager.Application.DTOs;

public record WebServiceMethodDto(
    Guid Id,
    Guid WebServiceId,
    string Path,
    string HttpMethod,
    string? RequestSchema,
    string? ResponseSchema,
    int TimeoutMs,
    string? RetryPolicyJson,
    string? CircuitPolicyJson,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ICollection<WebServiceParamDto>? Parameters = null
);

public record WebServiceMethodSummaryDto(
    Guid Id,
    string Path,
    string HttpMethod,
    int TimeoutMs,
    bool IsActive
);

public record CreateWebServiceMethodDto(
    string Path,
    string HttpMethod,
    string? RequestSchema = null,
    string? ResponseSchema = null,
    int TimeoutMs = 30000,
    string? RetryPolicyJson = null,
    string? CircuitPolicyJson = null
);

public record UpdateWebServiceMethodDto(
    string Path,
    string HttpMethod,
    string? RequestSchema = null,
    string? ResponseSchema = null,
    int TimeoutMs = 30000,
    string? RetryPolicyJson = null,
    string? CircuitPolicyJson = null
);

public record TestWebServiceMethodDto(
    Dictionary<string, object> Parameters
);