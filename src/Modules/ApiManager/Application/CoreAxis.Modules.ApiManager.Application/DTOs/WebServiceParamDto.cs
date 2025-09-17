using CoreAxis.Modules.ApiManager.Domain;

namespace CoreAxis.Modules.ApiManager.Application.DTOs;

public record WebServiceParamDto(
    Guid Id,
    Guid MethodId,
    string Name,
    ParameterLocation Location,
    string Type,
    bool IsRequired,
    string? DefaultValue,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record WebServiceParamSummaryDto(
    Guid Id,
    string Name,
    ParameterLocation Location,
    string Type,
    bool IsRequired,
    string? DefaultValue
);

public record CreateWebServiceParamDto(
    string Name,
    ParameterLocation Location,
    string Type,
    bool IsRequired = false,
    string? DefaultValue = null
);

public record UpdateWebServiceParamDto(
    string Name,
    ParameterLocation Location,
    string Type,
    bool IsRequired = false,
    string? DefaultValue = null
);