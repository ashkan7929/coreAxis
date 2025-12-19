using MediatR;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record ImportOpenApiCommand(string ServiceName, string OpenApiSpec, string BaseUrl) : IRequest<ImportOpenApiResult>;

public record ImportOpenApiResult(int MethodsImported, int MethodsUpdated);
