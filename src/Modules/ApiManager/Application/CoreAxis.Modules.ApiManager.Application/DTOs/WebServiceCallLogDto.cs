namespace CoreAxis.Modules.ApiManager.Application.DTOs;

public record WebServiceCallLogDto(
    Guid Id,
    Guid WebServiceId,
    Guid MethodId,
    string? CorrelationId,
    string? RequestDump,
    string? ResponseDump,
    int? StatusCode,
    long LatencyMs,
    bool Succeeded,
    string? Error,
    DateTime CreatedAt,
    string? WebServiceName = null,
    string? MethodPath = null,
    string? HttpMethod = null
);

public record WebServiceCallLogSummaryDto(
    Guid Id,
    string? CorrelationId,
    int? StatusCode,
    long LatencyMs,
    bool Succeeded,
    DateTime CreatedAt,
    string? WebServiceName = null,
    string? MethodPath = null
);

public record CallLogFilterDto(
    Guid? WebServiceId = null,
    Guid? MethodId = null,
    bool? Succeeded = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50
);