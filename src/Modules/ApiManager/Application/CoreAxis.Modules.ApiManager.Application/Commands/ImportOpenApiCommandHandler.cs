using CoreAxis.Modules.ApiManager.Domain;
using MediatR;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public class ImportOpenApiCommandHandler : IRequestHandler<ImportOpenApiCommand, ImportOpenApiResult>
{
    private readonly IWebServiceRepository _serviceRepository;
    private readonly IWebServiceMethodRepository _methodRepository;

    public ImportOpenApiCommandHandler(
        IWebServiceRepository serviceRepository,
        IWebServiceMethodRepository methodRepository)
    {
        _serviceRepository = serviceRepository;
        _methodRepository = methodRepository;
    }

    public async Task<ImportOpenApiResult> Handle(ImportOpenApiCommand request, CancellationToken cancellationToken)
    {
        var reader = new OpenApiStringReader();
        var document = reader.Read(request.OpenApiSpec, out var diagnostic);

        if (diagnostic.Errors.Count > 0)
        {
            throw new ArgumentException($"Invalid OpenAPI spec: {diagnostic.Errors.First().Message}");
        }

        // Get or create service
        var service = await _serviceRepository.GetByNameAsync(request.ServiceName, cancellationToken);
        if (service == null)
        {
            service = new WebService(
                request.ServiceName,
                request.BaseUrl,
                document.Info?.Description ?? "Imported from OpenAPI"
            );
            await _serviceRepository.AddAsync(service, cancellationToken);
        }
        else
        {
            service.Update(
                request.ServiceName,
                request.BaseUrl,
                !string.IsNullOrEmpty(document.Info?.Description) ? document.Info.Description : service.Description
            );
            await _serviceRepository.UpdateAsync(service, cancellationToken);
        }

        int imported = 0;
        int updated = 0;

        foreach (var pathItem in document.Paths)
        {
            foreach (var operation in pathItem.Value.Operations)
            {
                var methodType = operation.Key.ToString().ToUpper();
                var path = pathItem.Key;
                
                // Find method by path and verb
                var method = await _methodRepository.GetByServiceAndPathAsync(service.Id, path, methodType, cancellationToken);
                bool isNew = method == null;

                if (isNew)
                {
                    method = new WebServiceMethod(
                        service.Id,
                        path,
                        methodType,
                        timeoutMs: 30000
                    );
                }
                else
                {
                    method.Update(path, methodType, method.TimeoutMs);
                }
                
                if (isNew)
                {
                    await _methodRepository.AddAsync(method, cancellationToken);
                    imported++;
                }
                else
                {
                    await _methodRepository.UpdateAsync(method, cancellationToken);
                    updated++;
                }
            }
        }

        return new ImportOpenApiResult(imported, updated);
    }
}
