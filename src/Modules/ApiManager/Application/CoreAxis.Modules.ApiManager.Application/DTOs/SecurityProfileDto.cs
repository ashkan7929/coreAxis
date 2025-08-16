namespace CoreAxis.Modules.ApiManager.Application.DTOs;

public record SecurityProfileDto(
    Guid Id,
    string Type,
    string ConfigJson,
    string? RotationPolicy,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record SecurityProfileSummaryDto(
    Guid Id,
    string Type,
    string ConfigJson,
    string? RotationPolicy
);