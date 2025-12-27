using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Ports;
using MediatR;
using System.Text.Json;
using CoreAxisExecutionContext = CoreAxis.SharedKernel.Context.ExecutionContext;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Commands;

public record StartProductCommand(string ProductKey, JsonElement? Context = null) : IRequest<Result<StartProductResultDto>>;

public record StartProductResultDto(Guid WorkflowId, string WorkflowStatus);

public class StartProductCommandHandler : IRequestHandler<StartProductCommand, Result<StartProductResultDto>>
{
    private readonly IProductRepository _repository;
    private readonly IWorkflowClient _workflowClient;

    public StartProductCommandHandler(IProductRepository repository, IWorkflowClient workflowClient)
    {
        _repository = repository;
        _workflowClient = workflowClient;
    }

    public async Task<Result<StartProductResultDto>> Handle(StartProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByKeyAsync(request.ProductKey, cancellationToken);
        if (product == null)
        {
            return Result<StartProductResultDto>.Failure($"Product not found: {request.ProductKey}");
        }

        var version = await _repository.GetPublishedVersionAsync(product.Id, cancellationToken);
        if (version == null)
        {
            return Result<StartProductResultDto>.Failure($"No published version found for product: {request.ProductKey}");
        }

        if (version.Binding == null || string.IsNullOrEmpty(version.Binding.WorkflowDefinitionCode))
        {
             return Result<StartProductResultDto>.Failure($"Product version {version.VersionNumber} has no workflow binding");
        }

        var executionContext = new CoreAxisExecutionContext();

        // B) Populate ExecutionContext from request
        if (request.Context.HasValue)
        {
            try 
            {
                var json = request.Context.Value.GetRawText();
                executionContext.FormRawJson = json;
                executionContext.Form = JsonSerializer.Deserialize<JsonElement>(json);
            }
            catch 
            {
                executionContext.FormRawJson = "{}";
                executionContext.Form = new object();
            }
        }

        // Add product context to Vars
        executionContext.Vars["product"] = new 
        {
            key = product.Key,
            name = product.Name,
            version = version.VersionNumber,
            versionId = version.Id
        };

        // Populate Meta
        executionContext.Meta.ProductKey = product.Key;
        executionContext.Meta.TenantId = product.TenantId;
        executionContext.Meta.StartedAt = DateTimeOffset.UtcNow;
        executionContext.Meta.Trigger = "StartProductCommand";

        int? workflowVersion = null;
        if (int.TryParse(version.Binding.WorkflowVersionNumber, out var v))
        {
            workflowVersion = v;
        }

        var wfResult = await _workflowClient.StartAsync(version.Binding.WorkflowDefinitionCode, executionContext, workflowVersion, cancellationToken);
        
        if (!wfResult.IsSuccess)
        {
            return Result<StartProductResultDto>.Failure($"Workflow start failed: {wfResult.Error}");
        }

        return Result<StartProductResultDto>.Success(new StartProductResultDto(wfResult.WorkflowId, wfResult.Status));
    }
}
