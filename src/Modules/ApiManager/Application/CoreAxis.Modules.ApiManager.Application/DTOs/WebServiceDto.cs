namespace CoreAxis.Modules.ApiManager.Application.DTOs;

public record WebServiceDto(
    Guid Id,
    string Name,
    string BaseUrl,
    string? Description,
    Guid? SecurityProfileId,
    bool IsActive,
    string? OwnerTenantId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    SecurityProfileSummaryDto? SecurityProfile = null,
    ICollection<WebServiceMethodSummaryDto>? Methods = null
);

public record WebServiceSummaryDto(
    Guid Id,
    string Name,
    string BaseUrl,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateWebServiceDto(
    string Name,
    string BaseUrl,
    string? Description = null,
    Guid? SecurityProfileId = null,
    string? OwnerTenantId = null
);

public record UpdateWebServiceDto(
    string Name,
    string BaseUrl,
    string? Description = null,
    Guid? SecurityProfileId = null
);