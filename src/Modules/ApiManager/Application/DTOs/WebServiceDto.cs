using CoreAxis.Modules.ApiManager.Domain;

namespace CoreAxis.Modules.ApiManager.Application.DTOs;

public record WebServiceSummaryDto(
    Guid Id,
    string Name,
    string Description,
    string BaseUrl,
    bool IsActive,
    string? OwnerTenantId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? SecurityProfileType
);

public record WebServiceDetailsDto(
    Guid Id,
    string Name,
    string Description,
    string BaseUrl,
    bool IsActive,
    string? OwnerTenantId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    SecurityProfileDto? SecurityProfile,
    List<WebServiceMethodDto> Methods
);

public record WebServiceMethodDto(
    Guid Id,
    string Name,
    string Description,
    string Path,
    string HttpMethod,
    int TimeoutMs,
    string? RetryPolicyJson,
    string? CircuitPolicyJson,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<WebServiceParamDto> Parameters
);

public record WebServiceParamDto(
    Guid Id,
    string Name,
    string Description,
    ParameterLocation Location,
    string DataType,
    bool IsRequired,
    string? DefaultValue,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record WebServiceCallLogDto(
    Guid Id,
    Guid WebServiceId,
    Guid? MethodId,
    string? RequestBody,
    string? ResponseBody,
    int? StatusCode,
    bool IsSuccess,
    string? ErrorMessage,
    long LatencyMs,
    DateTime CalledAt,
    string WebServiceName,
    string? MethodSignature
);

public record SecurityProfileDto(
    Guid Id,
    string Type,
    string ConfigJson,
    string? RotationPolicy,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateWebServiceParamDto(
    string Name,
    string Description,
    ParameterLocation Location,
    string DataType,
    bool IsRequired,
    string? DefaultValue
);