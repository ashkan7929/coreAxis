using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public class ImportOpenApiCommandHandler : IRequestHandler<ImportOpenApiCommand, ImportOpenApiResult>
{
    private readonly IWebServiceRepository _serviceRepository;
    private readonly IWebServiceMethodRepository _methodRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportOpenApiCommandHandler(
        IWebServiceRepository serviceRepository,
        IWebServiceMethodRepository methodRepository,
        IUnitOfWork unitOfWork)
    {
        _serviceRepository = serviceRepository;
        _methodRepository = methodRepository;
        _unitOfWork = unitOfWork;
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
            await _serviceRepository.AddAsync(service);
        }
        else
        {
            service.Update(
                request.ServiceName,
                request.BaseUrl,
                !string.IsNullOrEmpty(document.Info?.Description) ? document.Info.Description : service.Description
            );
            _serviceRepository.Update(service);
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
                        webServiceId: service.Id,
                        path: path,
                        httpMethod: methodType,
                        timeoutMs: 30000
                    );
                }
                else
                {
                    method.Update(path, methodType, method.TimeoutMs);
                }
                
                if (isNew)
                {
                    await _methodRepository.AddAsync(method);
                    imported++;
                }
                else
                {
                    _methodRepository.Update(method);
                    updated++;
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return new ImportOpenApiResult(imported, updated);
    }
}
